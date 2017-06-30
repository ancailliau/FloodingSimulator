using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

using LtlSharp;
using LtlSharp.Monitoring;

using MoreLinq;
using UCLouvain.KAOSTools.MetaModel;
using NLog;
using System.Diagnostics;

namespace Simulator
{
	public class GoalMonitor
	{
		TimeSpan MonitoringDelay = TimeSpan.FromMinutes(1);

		/// <summary>
		/// The list of all probabilistic monitors.
		/// </summary>
		public Dictionary<KAOSMetaModelElement, ProbabilisticLTLMonitor> kaosElementMonitor;

		/// <summary>
		/// The projection used for computing a hash.
		/// </summary>
		public HashSet<string> projection;

		/// <summary>
		/// The buffer for the observed states.
		/// </summary>
		BufferBlock<CachedHashMonitoredState> buffer;

		/// <summary>
		/// The block processing the observed state to create new monitors.
		/// </summary>
		TransformBlock<CachedHashMonitoredState,CachedHashMonitoredState> createMonitors;

		/// <summary>
		/// The block processing the observed state to update monitors
		/// </summary>
		TransformBlock<CachedHashMonitoredState, CachedHashMonitoredState> updateMonitors;

		/// <summary>
		/// The block processing the observed state to update the transition relation, if monitored.
		/// </summary>
		ActionBlock<CachedHashMonitoredState> updateTransitionRelation;

		/// <summary>
		/// The block ending the dataflow TPL pipeline. Required for completion of the transform blocks.
		/// </summary>
		ActionBlock<CachedHashMonitoredState> dummyEnd;

		/// <summary>
		/// The current state, only used when saving the transition relation.
		/// </summary>
		int currentState = -1;

		/// <summary>
		/// The monitored transitions. Key is the source, value is (target, count).
		/// </summary>
		public Dictionary<int, Dictionary<int, int>> monitoredTransitions;

		/// <summary>
		/// The monitored states. Key is the hash, value is the first monitored state observed with that specific hash.
		/// </summary>
		Dictionary<int, CachedHashMonitoredState> monitoredStates;

		public Dictionary<int, MonitoredState> MonitoredStates {
			get {
				if (monitoredStates == null)
					return null;
				return monitoredStates.ToDictionary (kv => kv.Key, kv => (MonitoredState) kv.Value);
			}
		}

		static Logger logger = LogManager.GetCurrentClassLogger();

		public bool Ready { get; private set; }

		#region Private classes

		/// <summary>
		/// Private class for storing the hash computed on the projection of the monitored state
		/// </summary>
		class CachedHashMonitoredState : MonitoredState
		{
			public int StateHash;
			public DateTime Timestamp;

			public CachedHashMonitoredState (MonitoredState ms, DateTime now, HashSet<string> projection) : base (ms)
			{
				Timestamp = now;
				StateHash = GetProjectedHashCode (projection);
			}

			public int GetProjectedHashCode (HashSet<string> propositions)
			{
				unchecked {
					var localHash = 17;
					int factor = 23;
					foreach (var kv in state.Where (x => propositions.Contains (x.Key.Name))) {
						localHash += factor * (kv.Key.Name.GetHashCode () + kv.Value.GetHashCode ());
						factor *= 23;
					}

					return localHash;
				}
			}
		}

		#endregion

		public KAOSModel model;

		#region Constructors

		/// <summary>
		/// Creates a new monitoring infrastructure for the specified formulas. 
		/// The projection is used to compute the hash of the observed states.
		/// </summary>
		/// <param name="elements">The goals to monitor.</param>
		/// <param name="storage">The storage.</param>
		/// <param name="projection">Projection.</param>
		public GoalMonitor (KAOSModel model,
			IEnumerable<KAOSMetaModelElement> elements,
		                HashSet<string> projection,
		                    Func<KAOSMetaModelElement, IStateInformationStorage> storage,
		                    TimeSpan monitoringDelay)
		{
			Ready = false;
			this.model = model;
			this.MonitoringDelay = monitoringDelay;

			this.projection = projection;

			// Build all monitors
			kaosElementMonitor = new Dictionary<KAOSMetaModelElement, ProbabilisticLTLMonitor> ();
			foreach (var element in elements) {
				var w = Stopwatch.StartNew();
				logger.Info("Building monitor for " + element.FriendlyName);
				if (element is Goal && ((Goal)element).FormalSpec != null) {
					var goal = (Goal)element;
					if (goal.FormalSpec is StrongImply) {
						var casted = (StrongImply)goal.FormalSpec;
						var translatedFormula = TranslateToLtlSharp(new Imply(casted.Left, casted.Right));
						kaosElementMonitor.Add(goal, new ProbabilisticLTLMonitor(translatedFormula, storage(goal)));
						logger.Trace("Formula {0} converted to {1}", goal.FormalSpec, translatedFormula.Normalize());
					} else if (goal.FormalSpec is KAOSTools.MetaModel.Globally) {
						var casted = (KAOSTools.MetaModel.Globally)goal.FormalSpec;
						var translatedFormula = TranslateToLtlSharp(casted.Enclosed);
						kaosElementMonitor.Add(goal, new ProbabilisticLTLMonitor(translatedFormula, storage(goal)));
						logger.Trace("Formula {0} converted to {1}", goal.FormalSpec, translatedFormula.Normalize());
					} else {
						throw new NotSupportedException(
							"Goals must follow the pattern G(phi) where phi is an LTL formula."
						);
					}
				}
				if (element is Obstacle && ((Obstacle)element).FormalSpec != null) {
					var obstacle = (Obstacle)element;
					if (obstacle.FormalSpec is Eventually) {
						var casted = (Eventually)obstacle.FormalSpec;
						var translatedFormula = TranslateToLtlSharp(casted.Enclosed);
						kaosElementMonitor.Add(obstacle, new ProbabilisticLTLMonitor(translatedFormula, storage(obstacle)));
						logger.Trace("Formula {0} converted to {1}", obstacle.FormalSpec, translatedFormula.Normalize());
					} else {
						throw new NotSupportedException(
							"Obstacle must follow the pattern F(phi) where phi is an LTL formula."
						);
					}
				}
				w.Stop();
				logger.Info("Time to build monitor: {0}ms", w.ElapsedMilliseconds);
			}
		}

		#endregion

		#region Goal to LTLSharp

		ITLFormula TranslateToLtlSharp(Formula formalSpec)
		{
			if (formalSpec is StrongImply) {
				var casted = ((StrongImply)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Globally(new LtlSharp.Implication (left, right));

			} else if (formalSpec is KAOSTools.MetaModel.Imply) {
				var casted = ((KAOSTools.MetaModel.Imply)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Implication(left, right);

			} else if (formalSpec is KAOSTools.MetaModel.Until) {
				var casted = ((KAOSTools.MetaModel.Until)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Until(left, right);

			} else if (formalSpec is KAOSTools.MetaModel.Release) {
				var casted = ((KAOSTools.MetaModel.Release)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Release(left, right);

			} else if (formalSpec is KAOSTools.MetaModel.Unless) {
				var casted = ((KAOSTools.MetaModel.Unless)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Unless(left, right);

			} else if (formalSpec is And) {
				var casted = ((KAOSTools.MetaModel.And)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Conjunction(left, right);

			} else if (formalSpec is Or) {
				var casted = ((KAOSTools.MetaModel.Or)formalSpec);
				var left = TranslateToLtlSharp(casted.Left);
				var right = TranslateToLtlSharp(casted.Right);
				return new LtlSharp.Disjunction(left, right);

			} else if (formalSpec is Not) {
				var casted = ((KAOSTools.MetaModel.Not)formalSpec);
				var enclosed = TranslateToLtlSharp(casted.Enclosed);
				return new LtlSharp.Negation(enclosed);

			} else if (formalSpec is KAOSTools.MetaModel.Next) {
				var casted = ((KAOSTools.MetaModel.Not)formalSpec);
				var enclosed = TranslateToLtlSharp(casted.Enclosed);
				return new LtlSharp.Negation(enclosed);

			} else if (formalSpec is Eventually) {
				var casted = ((KAOSTools.MetaModel.Eventually)formalSpec);
				var enclosed = TranslateToLtlSharp(casted.Enclosed);
				if (casted.TimeBound == null) 
					return new LtlSharp.Finally(enclosed);
				else
					return new LtlSharp.BoundedFinally(enclosed, ConvertBound (casted.TimeBound));

			} else if (formalSpec is EventuallyBefore) {
				throw new NotImplementedException();
				//var casted = ((KAOSTools.MetaModel.EventuallyBefore)formalSpec);
				//var enclosed = TranslateToLtlSharp(casted.Enclosed);

				//// todo check for the comparison operator
				//return new LtlSharp.BoundedFinally(enclosed, ConvertBound(casted.TimeBound));

			} else if (formalSpec is KAOSTools.MetaModel.Globally) {
				var casted = ((KAOSTools.MetaModel.Globally)formalSpec);
				if (casted.TimeBound != null) {
					var enclosed = TranslateToLtlSharp(casted.Enclosed);
					return new LtlSharp.BoundedGlobally(enclosed, ConvertBound (casted.TimeBound));
					
				} else {
					var enclosed = TranslateToLtlSharp(casted.Enclosed);
					return new LtlSharp.Globally(enclosed);
				}

			} else if (formalSpec is PredicateReference) {
				var casted = ((KAOSTools.MetaModel.PredicateReference)formalSpec);
				return new LtlSharp.Proposition(casted.Predicate.FriendlyName);
			}

			throw new NotImplementedException(string.Format ("Operator {0} is not translatable to LTLSharp framework.",
			                                                 formalSpec.GetType ().FullName));
		}

		int ConvertBound(TimeBound span)
		{
			// todo not the safest...
			// todo issue warning if division is not an integer
			var value = (int)Math.Ceiling(span.Bound.TotalMilliseconds / MonitoringDelay.TotalMilliseconds);
			logger.Trace("Convertion of {0} to {1}", span.Bound.ToString(), value);
			logger.Trace("Monitoring Delay : {0}", MonitoringDelay);
			return value;
		}

		#endregion

		/// <summary>
		/// Run the monitors and save a summary every second in summaryFilename.
		/// </summary>
		/// <param name="saveTransitionRelation">Whether the transition relation shall be kept.</param>
		public void Run (bool saveTransitionRelation = false)
		{
			monitoredTransitions = new Dictionary<int, Dictionary<int, int>> ();
			monitoredStates = new Dictionary<int, CachedHashMonitoredState> ();

			buffer = new BufferBlock<CachedHashMonitoredState> ();
			createMonitors = new TransformBlock<CachedHashMonitoredState, CachedHashMonitoredState> ((ms) => CreateNewMonitors(ms));
			updateMonitors = new TransformBlock<CachedHashMonitoredState, CachedHashMonitoredState> ((ms) => UpdateMonitors (ms));
			if (!saveTransitionRelation) {
				dummyEnd = new ActionBlock<CachedHashMonitoredState> (_ => { });
			} else {
				updateTransitionRelation = new ActionBlock<CachedHashMonitoredState> ((ms) => UpdateMonitoredTransitionRelation (ms));
			}

			var opt = new DataflowLinkOptions ();
			opt.PropagateCompletion = true;

			buffer.LinkTo (createMonitors, opt);
			createMonitors.LinkTo (updateMonitors, opt);
			if (!saveTransitionRelation) {
				updateMonitors.LinkTo (dummyEnd, opt);
			} else {
				updateMonitors.LinkTo (updateTransitionRelation);
			}

			Ready = true;
		}

		public void Stop ()
		{
			buffer.Complete ();
			createMonitors.Completion.Wait ();
			updateMonitors.Completion.Wait ();
		}

		/// <summary>
		/// Adds the monitored state to the processing pipeline.
		/// </summary>
		/// <param name="currentState">The monitored state.</param>
		public void MonitorStep (MonitoredState currentState, DateTime now)
		{
			var cachedMs = new CachedHashMonitoredState (currentState, now, projection);

			//Console.WriteLine ("*** [" + cachedMs.Timestamp.ToString ("hh:mm:ss.fff") + "] SAMPLE (" + string.Join (",", cachedMs.state.Select (kv => kv.Key + ":" + kv.Value)) + ")");
			//Console.WriteLine ("***  " + cachedMs.StateHash);

			buffer.Post (cachedMs);
		}

		/// <summary>
		/// Updates the monitored transition relation.
		/// </summary>
		/// <returns>The monitored state.</returns>
		/// <param name="ms">The monitored state.</param>
		CachedHashMonitoredState UpdateMonitoredTransitionRelation (CachedHashMonitoredState ms)
		{
			try {
				int stateIdentifier = ms.StateHash;
				if (currentState != -1) {
					if (!monitoredTransitions.ContainsKey (currentState)) {
						monitoredTransitions.Add (currentState, new Dictionary<int, int> ());
					}

					if (!monitoredTransitions [currentState].ContainsKey (stateIdentifier)) {
						monitoredTransitions [currentState].Add (stateIdentifier, 0);
					}
					monitoredTransitions [currentState] [stateIdentifier]++;
				}

				if (!monitoredStates.ContainsKey (stateIdentifier)) {
					monitoredStates.Add (stateIdentifier, ms);
				}
				currentState = stateIdentifier;


			} catch (Exception e) {
				Console.WriteLine (e);
			}

			return ms;
		}

		/// <summary>
		/// Starts a new monitors for the specified state.
		/// </summary>
		/// <returns>The monitored state.</returns>
		/// <param name="ms">The monitored state.</param>
		CachedHashMonitoredState CreateNewMonitors (CachedHashMonitoredState ms)
		{
			try {
				foreach (var m in kaosElementMonitor) {
					if (m.Key.CustomData.ContainsKey("hashProjection")) {
						//logger.Info("Has hashProjection");
						var p = m.Key.CustomData["hashProjection"].Split(',').ToHashSet ();
						m.Value.StartNew(ms.StateHash, ms, p);
					} else {
						//logger.Info("Dont have hashProjection");
						m.Value.StartNew(ms.StateHash, ms);
					}
				}

			} catch (Exception e) {
				Console.WriteLine (e);
			}
			return ms;
		}

		/// <summary>
		/// Updates the monitors for the specified state.
		/// </summary>
		/// <returns>The monitored state.</returns>
		/// <param name="ms">The monitored state.</param>
		CachedHashMonitoredState UpdateMonitors (CachedHashMonitoredState ms)
		{
			//var watch = Stopwatch.StartNew ();
			try {

				foreach (var m in kaosElementMonitor) {
					m.Value.Step (ms);
				}

			} catch (Exception e) {
				Console.WriteLine (e);
			}

			//watch.Stop ();
			// Console.WriteLine ("Time to update monitors: {0:0.##}ms", watch.ElapsedMilliseconds);

			// Console.WriteLine ("Updated: " + ms.Timestamp.ToString ("mm:ss.fff"));

			return ms;
		}
	}
}


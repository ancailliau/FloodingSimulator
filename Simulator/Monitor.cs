using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

using LtlSharp;
using LtlSharp.Monitoring;

using MoreLinq;

namespace Simulator
{

	public class MonitoredFormula
	{
		public ITLFormula formula;
		public string Name;
		public int expectedPositives;
		public int expectedNegatives;
	}

	public class Monitor
	{	
		/// <summary>
		/// The list of all probabilistic monitors.
		/// </summary>
		public List<ProbabilisticLTLMonitor> ltlMonitors;

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

		#region Constructors

		/// <summary>
		/// Creates a new monitoring infrastructure for the specified formulas. 
		/// The default projection is build using all proposition appearing in the
		/// formulas.
		/// </summary>
		/// <param name="storage">The storage.</param>
		/// <param name="formula">Formula.</param>
		public Monitor (IEnumerable<MonitoredFormula> formula,
						Func<IStateInformationStorage> storage)
			: this (formula, formula.SelectMany (x => x.formula.Propositions).Select (x => x.Name).ToHashSet (), storage)
		{ }

		/// <summary>
		/// Creates a new monitoring infrastructure for the specified formulas. 
		/// The projection is used to compute the hash of the observed states.
		/// </summary>
		/// <param name="formula">Formula.</param>
		/// <param name="storage">The storage.</param>
		/// <param name="projection">Projection.</param>
		public Monitor (IEnumerable<MonitoredFormula> formula,
		                HashSet<string> projection,
		                Func<IStateInformationStorage> storage)
		{
			Ready = false;

			this.projection = projection;

			// Build all monitors
			ltlMonitors = new List<ProbabilisticLTLMonitor> ();
			foreach (var f in formula) {
				ltlMonitors.Add (new ProbabilisticLTLMonitor (f.formula, storage ()));
			}
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
				foreach (var m in ltlMonitors) {
					m.StartNew (ms.StateHash, ms);
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

				foreach (var m in ltlMonitors) {
					m.Step (ms);
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


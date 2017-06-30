using System;
using System.Drawing;
using NLog;
using System.Text;

namespace UCLouvain.FloodingSystem
{
	public class CustomBitmap
	{
		static Logger logger = LogManager.GetCurrentClassLogger();
		private int[,] image;

		public int Width { get; private set; }
		public int Height { get; private set; }

		public CustomBitmap(int width, int height)
		{
			Width = width;
			Height = height;
			//logger.Info("created");
			image = new int[width, height];
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					image[i, j] = 0;
				}
			}
			//logger.Info("initiated");
		}

		public void SetPixel(int x, int y, int color)
		{
			image[x, y] = color;
		}

		public int GetPixel(int x, int y)
		{
			return image[x, y];
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					sb.Append(image[i, j]);
				}
				sb.Append("\n");
			}

			return sb.ToString();
		}
	}
}


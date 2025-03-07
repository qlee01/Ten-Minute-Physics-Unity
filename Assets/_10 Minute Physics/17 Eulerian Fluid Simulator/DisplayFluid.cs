using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace EulerianFluidSimulator
{
	//Display the fluid simulation data on a texture
	//Display streamlines and velocities with lines
	//Display obstacles as mesh
	public static class DisplayFluid
	{
		private static Mesh circleMesh;
		private static float circleRadius = 0f;



		//For testing
		public static void TestDraw(FluidScene scene)
		{
			if (scene.fluidTexture == null)
			{
				scene.fluidTexture = new(4, 2);

				//So the pixels dont blend
				scene.fluidTexture.filterMode = FilterMode.Point;

				scene.fluidTexture.wrapMode = TextureWrapMode.Clamp;

				scene.fluidMaterial.mainTexture = scene.fluidTexture;
			}
	
			//The colors array is a flattened 2D array, where pixels are laid out left to right, bottom to top, which fits how the fluid simulation arrays are set up
			//Color32 is 0->255 (byte)
			//Color is 0->1 (float)
			Color32[] colors = new Color32[8];

			//This works even though Color is 0->1 because it converts float->byte
			colors[0] = Color.black; //BL 
			colors[1] = Color.white;
			colors[2] = Color.white;
			colors[3] = Color.blue; //BR

			colors[4] = Color.red; //TL
			colors[5] = Color.white;
			colors[6] = Color.white;
			colors[7] = Color.green; //TR

			scene.fluidTexture.SetPixels32(colors);

			scene.fluidTexture.Apply();

			//Debug.Log(colors[4]); //RGBA(255, 0, 0, 255)
		}



		//Called every Update
		public static void Draw(FluidScene scene)
		{
			UpdateTexture(scene);

			if (scene.showVelocities)
			{
				ShowVelocities(scene);
			}

			//scene.showStreamlines = true;

			if (scene.showStreamlines)
			{
				ShowStreamlines(scene);
			}

			if (scene.showObstacle)
			{
				ShowObstacle(scene);
			}

			//Moved the display of min and max pressure as text to the UI class
		}



		//
		// Show the fluid simulation data on a texture
		//
		private static void UpdateTexture(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			Texture2D fluidTexture = scene.fluidTexture;

			//Generate a new texture if none exists or if we have changed resolution
			if (fluidTexture == null || fluidTexture.width != f.numX || fluidTexture.height != f.numY)
			{
				fluidTexture = new(f.numX, f.numY);

				//Dont blend the pixels
				//fluidTexture.filterMode = FilterMode.Point;

				//Blend the pixels 
				fluidTexture.filterMode = FilterMode.Bilinear;

				//Don't wrap the border with the border on the opposite side of the texture
				fluidTexture.wrapMode = TextureWrapMode.Clamp;

				scene.fluidMaterial.mainTexture = fluidTexture;
				
				scene.fluidTexture = fluidTexture;
			}


			//The texture colors
			Color32[] textureColors = new Color32[f.numX * f.numY];

			//Find min and max pressure
			MinMax minMaxP = f.GetMinMaxPressure();

			//Find the colors
			//This was an array in the source, but we can treat the Vector4 as an array to make the code match
			//Vector4 color = new (255, 255, 255, 255);

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					//This was an array in the source, but we can treat the Vector4 as an array to make the code match
					//Moved to here from before the loop so it resets every time so we can display the walls if we deactivate both pressure and smoke
					Vector4 color = new(255, 255, 255, 255);

					if (scene.showPressure)
					{
						float p = f.p[f.To1D(i, j)];

						//Blue means low pressure and red is high pressure
						color = GetSciColor(p, minMaxP.min, minMaxP.max);

						//Color the smoke according to the scientific color scheme 
						//Everything that's not smoke becomes black
						//Everything that's smoke shows the pressure field
						if (scene.showSmoke)
						{
							//How much smoke in this cell?
							float smoke = f.m[f.To1D(i, j)];

							//smoke = 0 means max smoke, so will be 0 if no smoke in the cell (smoke = 1)
							color[0] = Mathf.Max(0f, color[0] - 255 * smoke);
							color[1] = Mathf.Max(0f, color[1] - 255 * smoke);
							color[2] = Mathf.Max(0f, color[2] - 255 * smoke);
						}
					}
					else if (scene.showSmoke)
					{
						//How much smoke in this cell?
						float smoke = f.m[f.To1D(i, j)];

						//smoke = 0 means max smoke, and 255 * 0 = 0 -> black 
						color[0] = 255 * smoke;
						color[1] = 255 * smoke;
						color[2] = 255 * smoke;

						//In the paint scene we color the smoke according to the scientific color scheme
						if (scene.sceneNr == FluidScene.SceneNr.Paint)
						{
							color = GetSciColor(smoke, 0f, 1f);
						}
					}
					//If both pressure and smoke are deactivated, then display obstacles as black, the rest as white
					//There was a bug in the source code where everything turned back, but "f.s[f.To1D(i, j)] == 0f" should mean only walls should be black
					else if (f.s[f.To1D(i, j)] == 0f)
                    {
                        color[0] = 0;
                        color[1] = 0;
						color[2] = 0;
                    }

                    //Add the color to this pixel
                    //Color32 is 0-255
                    Color32 pixelColor = new((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]);

					textureColors[f.To1D(i, j)] = pixelColor;
				}
			}
			
			//Add all colors to the texture
			fluidTexture.SetPixels32(textureColors);

			//Copies changes you've made in a CPU texture to the GPU
			fluidTexture.Apply(false);
		}
	


		//
		// Show the u and v velocities at each cell by drawing lines
		//
		private static void ShowVelocities(FluidScene scene)
		{
			FluidSim f = scene.fluid;

			//Cell width
			float h = f.h;

			//The length of the lines which will be scaled by the velocity in simulation space
			float scale = 0.02f;

			List<Vector3> linesToDisplay = new ();

			//So the lines are drawn infront of the simulation plane
			float z = -0.01f;

			for (int i = 0; i < f.numX; i++)
			{
				for (int j = 0; j < f.numY; j++)
				{
					float u = f.u[f.To1D(i, j)];
					float v = f.v[f.To1D(i, j)];


					//u velocity
					float x0 = i * h;
					float x1 = i * h + u * scale;
					float y = (j + 0.5f) * h; //the u vel is in the middle of the cell in y direction, thus the 0.5

					Vector2 uStart = scene.SimToWorld(x0, y);
					Vector2 uEnd = scene.SimToWorld(x1, y);

					linesToDisplay.Add(new Vector3(uStart.x, uStart.y, z));
					linesToDisplay.Add(new Vector3(uEnd.x, uEnd.y, z));


					//v velocity
					float x = (i + 0.5f) * h;
					float y0 = j * h;
					float y1 = j * h + v * scale;

					Vector2 vStart = scene.SimToWorld(x, y0);
					Vector2 vEnd = scene.SimToWorld(x, y1);

					linesToDisplay.Add(new Vector3(vStart.x, vStart.y, z));
					linesToDisplay.Add(new Vector3(vEnd.x, vEnd.y, z));
				}
			}

			//Display the lines with some black color
			DisplayShapes.DrawLineSegments(linesToDisplay, DisplayShapes.ColorOptions.Black);
		}

		

		//
		// Show streamlines that follows the velocity to easier visualize how the fluid flows
		//
		private static void ShowStreamlines(FluidScene scene)
		{		
			FluidSim f = scene.fluid;

			//How many segments per streamline?
			int numSegs = 15;

			List<Vector3> streamlineCoordinates = new ();

			//To display the line infront of the plane
			float z = -0.01f;

			//Dont display a streamline from each cell because it makes it difficult to see, so every 5 cell
			for (int i = 1; i < f.numX - 1; i += 5)
			{
				for (int j = 1; j < f.numY - 1; j += 5)
				{
					//Reset
					streamlineCoordinates.Clear();

					//Center of the cell in simulation space
					float x = (i + 0.5f) * f.h;
					float y = (j + 0.5f) * f.h;

					//Simulation space to global
					Vector2 startPos = scene.SimToWorld(x, y);

					streamlineCoordinates.Add(new Vector3(startPos.x, startPos.y, z));

					//Build the line
					for (int n = 0; n < numSegs; n++)
					{
						//The velocity at the current coordinate
						float u = f.SampleField(x, y, FluidSim.SampleArray.uField);
						float v = f.SampleField(x, y, FluidSim.SampleArray.vField);
						
						//Move a small step in the direction of the velocity
						x += u * 0.01f;
						y += v * 0.01f;

						//Stop the line if we are outside of the simulation area
						//The guy in the video is only checking x > f.GetWidth() for some reason...
						if (x > f.SimWidth || x < 0f || y > f.SimHeight || y < 0f)
						{
							break;
						}

						//Add the next coordinate of the streamline
						Vector2 nextPos2D = scene.SimToWorld(x, y);

						streamlineCoordinates.Add(new Vector3(nextPos2D.x, nextPos2D.y, z));
					}

					//Display the line
					DisplayShapes.DrawLine(streamlineCoordinates, DisplayShapes.ColorOptions.Black);
				}
			}
		}
		


		//
		// Display the circle obstacle
		//
		private static void ShowObstacle(FluidScene scene)
		{		
			FluidSim f = scene.fluid;

			//Make it slightly bigger to hide the jagged edges we get because we use a grid with square cells which will not match the circle edges prefectly
			float circleRadius = scene.obstacleRadius + f.h;
			
			//The color of the circle
			DisplayShapes.ColorOptions color = DisplayShapes.ColorOptions.Gray;

			//Black like the bg to make it look nicer
			if (scene.showPressure)
			{
				color = DisplayShapes.ColorOptions.Black;
			}

			//Circle center in global space
			Vector2 globalCenter2D = scene.SimToWorld(scene.obstacleX, scene.obstacleY);

			//3d space infront of the texture
			Vector3 circleCenter = new (globalCenter2D.x, globalCenter2D.y, -0.1f);

			//Generate a new circle mesh if we havent done so before or radius has changed 
			if (circleMesh == null || DisplayFluid.circleRadius != circleRadius)
			{
                circleMesh = DisplayShapes.GenerateCircleMesh_XY(Vector3.zero, circleRadius, 50);

				DisplayFluid.circleRadius = circleRadius;
            }

			//Display the circle mesh
			Material material = DisplayShapes.GetMaterial(color);

			Graphics.DrawMesh(circleMesh, circleCenter, Quaternion.identity, material, 0, Camera.main, 0);


			//The guy is also giving the circle a black border, which we could replicate by drawing a smaller circle but it doesn't matter! 
		}



		//
		// The scientific color scheme
		//

		//Get a color from a color gradient which is colored according to the scientific color scheme
		//This color scheme is also called rainbow (jet) or hot-to-cold
		//Similar to HSV color mode where we change the hue (except the purple part)
		//Rainbow is a linear interpolation between (0,0,255) and (255,0,0) in RGB color space (ignoring the purple part which would loop the circle like in HSV)
		//https://stackoverflow.com/questions/7706339/grayscale-to-red-green-blue-matlab-jet-color-scale
		private static Vector4 GetSciColorOriginal(float val, float minVal, float maxVal)
		{
			//Clamp val to be within the range
			//val has to be less than maxVal or "int num = Mathf.FloorToInt(val / m);" wont work
			//This will not always work because of floating point precision issues, so we have to fix it later
			val = Mathf.Min(Mathf.Max(val, minVal), maxVal - 0.0001f);

            //Convert to 0->1 range
            float d = maxVal - minVal;
		
			//If min and max are the same, set val to be in the middle or we get a division by zero
			val = (d == 0f) ? 0.5f : (val - minVal) / d;

			//0.25 means 4 buckets 0->3
			//Why 4? A walk on the edges of the RGB color cube: blue -> cyan -> green -> yellow -> red
			float m = 0.25f;

			int num = Mathf.FloorToInt(val / m);

			//Clamp num
			//If val = maxVal, we will end up in bucket 4
			//And we can't make val smaller because of floating point precision issues
			if (num > 3)
			{
				num = 3;
            }

			//s is strength
			float s = (val - num * m) / m;

			float r = 0f;
			float g = 0f;
			float b = 0f;

			//blue -> green -> yellow -> red
			switch (num)
			{
				case 0: r = 0f; g = s;      b = 1f;     break; //blue   (0,0,1) -> cyan   (0,1,1)
				case 1: r = 0f; g = 1f;     b = 1f - s; break; //cyan   (0,1,1) -> green  (0,1,0)
				case 2: r = s;  g = 1f;     b = 0f;     break; //green  (0,1,0) -> yellow (1,1,0)
				case 3: r = 1f; g = 1f - s; b = 0f;     break; //yellow (1,1,0) -> red    (1,0,0)
			}

			Vector4 color = new ( 255 * r, 255 * g, 255 * b, 255);

			//Vector4 color = new(255, 0, 0, 255);

			return color;
		}



		//Faster method to generate the Rainbow color scheme
		//Was generated by ChatGPT
		//Is also faster than a lookup table
		public static Vector4 GetSciColor(float value, float minVal, float maxVal)
		{
			value = Mathf.InverseLerp(minVal, maxVal, value);

			float r, g, b;

			if (value < 0.25f)
			{
				r = 0f;
				g = 4f * value;
				b = 1f;
			}
			else if (value < 0.5f)
			{
				r = 0f;
				g = 1f;
				b = 1f - 4f * (value - 0.25f);
			}
			else if (value < 0.75f)
			{
				r = 4f * (value - 0.5f);
				g = 1f;
				b = 0f;
			}
			else
			{
				r = 1f;
				g = 1f - 4f * (value - 0.75f);
				b = 0f;
			}

			Vector4 color = new(r * 255, g * 255, b * 255, 255);

			return color;
		}
	}
}

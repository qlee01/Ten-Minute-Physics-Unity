using EulerianFluidSimulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLIPFluidSimulator
{
    public class FLIPFluidUI
    {
        private readonly FLIPFluidSimController controller;

        //For mouse drag
        private Vector2 lastMousePos;



        public FLIPFluidUI(FLIPFluidSimController controller)
        {
            this.controller = controller;
        }



        public void Interaction(FLIPFluidScene scene)
        {
            //Teleport obstacle if we click with left mouse
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = GetMousePos(scene);

                //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                if (scene.fluid.IsWithinArea(mousePos.x, mousePos.y))
                {
                    controller.SetObstacle(mousePos.x, mousePos.y, true);

                    this.lastMousePos = mousePos;
                }
            }
            //Drag obstacle if we hold down left mouse
            else if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = GetMousePos(scene);

                //Has the mouse positioned not changed = we are not dragging?
                if (!(mousePos.x != this.lastMousePos.x && mousePos.y != this.lastMousePos.y))
                {
                    return;
                }

                //Is this coordinate within the simulation space (Or we will move the object when trying to interact with the UI)
                if (scene.fluid.IsWithinArea(mousePos.x, mousePos.y))
                {
                    controller.SetObstacle(mousePos.x, mousePos.y, false);

                    this.lastMousePos = mousePos;
                }

                scene.isPaused = false;
            }



            ////Pause the simulation
            //if (Input.GetKeyDown(KeyCode.P))
            //{
            //    scene.isPaused = !scene.isPaused;
            //}
            ////Move the simulation one step forward
            //else if (Input.GetKeyDown(KeyCode.M))
            //{
            //    scene.isPaused = false;

            //    controller.Simulate();

            //    scene.isPaused = true;
            //}



            //SampleCellWithMouse(scene);
        }



        //Sample the cells with the mouse position
        //Wasnt included in the tutorial but makes it easier to understand what's going on
        //private void SampleCellWithMouse(FLIPFluidScene scene)
        //{
        //    Vector2 mousePos = GetMousePos(scene);

        //    Vector2Int cellPos = scene.SimToCell(mousePos.x, mousePos.y);

        //    //Debug.Log(cellPos);

        //    FLIPFluidSim f = scene.fluid;

        //    int x = cellPos.x;
        //    int y = cellPos.y;

        //    if (x >= 0 && x < f.numX && y >= 0 && y < f.numY)
        //    {
        //        float velU = f.u[f.To1D(x, y)]; //velocity in u direction
        //        float velV = f.v[f.To1D(x, y)]; //velocity in v direction
        //        float p = f.p[f.To1D(x, y)]; //pressure
        //        float s = f.s[f.To1D(x, y)]; //solid (0) or fluid (1)
        //        float m = f.m[f.To1D(x, y)]; //smoke density

        //        int decimals = 3;

        //        velU = (float)System.Math.Round((decimal)velU, decimals);
        //        velV = (float)System.Math.Round((decimal)velV, decimals);
        //        p = (float)System.Math.Round((decimal)p, decimals);
        //        m = (float)System.Math.Round((decimal)m, decimals);

        //        //bool isSolid = (s == 0f);

        //        Debug.Log($"u: {velU}, v: {velV}, p: {p}, s: {s}, m: {m}");
        //    }
        //}



        //Get the mouse coordinates in simulation space
        private Vector2 GetMousePos(FLIPFluidScene scene)
        {
            //Default if raycasting doesnt work - which it always should
            Vector2 mousePos = Vector2.zero;

            //Fire a ray against a plane to get the position of the mouse in world space
            Plane plane = new(-Vector3.forward, Vector3.zero);

            //Create a ray from the mouse click position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float enter))
            {
                //Get the point that is clicked in world space
                Vector3 mousePos3D = ray.GetPoint(enter);

                //Debug.Log(mousePos);

                //From world space to simulation space
                mousePos = scene.WorldToSim(mousePos3D.x, mousePos3D.y);
            }

            return mousePos;
        }
    }
}
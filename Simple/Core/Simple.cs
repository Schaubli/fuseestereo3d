﻿#define GUI_SIMPLE

using System;
using System.IO;
using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Math.Core;
using Fusee.Serialization;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;

#if GUI_SIMPLE
using Fusee.Engine.Core.GUI;
#endif

namespace Fusee.Engine.Examples.Simple.Core
{

    [FuseeApplication(Name = "VR Example", Description = "A very simple VR example.")]
    public class Simple : RenderCanvas
    {
        static readonly object _syncLock = new object();
        public Stereo3D _stereo3d;
        // angle variables
        private static float _angleHorz = 0, _angleVert, _angleVelHorz, _angleVelVert;
        private float pi = 3.1415f;
        private const float RotationSpeed = 7;
        private const float Damping = 0.8f;
        int eyeDistance = 10;

        private SceneContainer _rocketScene;
        private SceneRenderer _sceneRenderer;
        
        private bool _keys;

        #if GUI_SIMPLE
        private GUIHandler _guiHandler;

        private GUIButton _guiFuseeLink;
        private GUIImage _guiFuseeLogo;
        private FontMap _guiLatoBlack;
        private GUIText _guiSubText;
        private float _subtextHeight;
        private float _subtextWidth;
        #endif
        public static float[] gameRotationVector = new float[3];
        private bool renderStereo = false;


        // Init is called on startup. 
        public override void Init()
        {
            if (Width <= 0)
            {
                Width = 2560;
            }
            if (Height <= 0)
            {
                Height = 1440;
            }
            #if GUI_SIMPLE
            _guiHandler = new GUIHandler();
            _guiHandler.AttachToContext(RC);
            _guiFuseeLink = new GUIButton(6, 6, 157, 87);
            _guiFuseeLink.ButtonColor = new float4(0, 0, 0, 0);
            _guiFuseeLink.BorderColor = new float4(0, 0.6f, 0.2f, 1);
            _guiFuseeLink.BorderWidth = 0;
            _guiFuseeLink.OnGUIButtonDown += _guiFuseeLink_OnGUIButtonDown;
            _guiFuseeLink.OnGUIButtonEnter += _guiFuseeLink_OnGUIButtonEnter;
            _guiFuseeLink.OnGUIButtonLeave += _guiFuseeLink_OnGUIButtonLeave;
            _guiHandler.Add(_guiFuseeLink);
            _guiFuseeLogo = new GUIImage(AssetStorage.Get<ImageData>("FuseeLogo150.png"), 10, 10, -5, 150, 80);
            _guiHandler.Add(_guiFuseeLogo);
            var fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            fontLato.UseKerning = true;
            _guiLatoBlack = new FontMap(fontLato, 18);
            _guiSubText = new GUIText("Simple FUSEE Cardboard Example", _guiLatoBlack, 100, 100);
            _guiSubText.TextColor = new float4(0.05f, 0.25f, 0.15f, 0.8f);
            _guiHandler.Add(_guiSubText);
            _subtextWidth = GUIText.GetTextWidth(_guiSubText.Text, _guiLatoBlack);
            _subtextHeight = GUIText.GetTextHeight(_guiSubText.Text, _guiLatoBlack);
            #endif

            // Set the clear color for the backbuffer to white (100% intentsity in all color channels R, G, B, A).
            RC.ClearColor = new float4(1, 1, 1, 1);
            
            if(renderStereo) { 
                _stereo3d = new Stereo3D(Stereo3DMode.Cardboard, Width, Height);
                _stereo3d.AttachToContext(RC);
            }

            // Load the rocket model
            _rocketScene = AssetStorage.Get<SceneContainer>("WuggyLand.fus");

            // Wrap a SceneRenderer around the model.
            _sceneRenderer = new SceneRenderer(_rocketScene);
        }
       
        // RenderAFrame is called once a frame
        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            // Mouse and keyboard movement
            if (Keyboard.LeftRightAxis != 0 || Keyboard.UpDownAxis != 0)
            {
                _keys = true;
            }

            if (Mouse.LeftButton)
            {
                _keys = false;
                _angleVelHorz = -RotationSpeed * Mouse.XVel * DeltaTime * 0.0005f;
                _angleVelVert = -RotationSpeed * Mouse.YVel * DeltaTime * 0.0005f;
            }
            else if (Touch.GetTouchActive(TouchPoints.Touchpoint_0))
            {
                //Reset view on touch 
                _angleHorz = gameRotationVector[0];
                _angleVert = gameRotationVector[2];
            }
            else
            {
                if (_keys)
                {
                    _angleVelHorz = -RotationSpeed * Keyboard.LeftRightAxis * DeltaTime;
                    _angleVelVert = -RotationSpeed * Keyboard.UpDownAxis * DeltaTime;
                }
                else
                {
                    var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);
                    _angleVelHorz *= curDamp;
                    _angleVelVert *= curDamp;
                }
            }
            

            //Rotate Scene
            _angleHorz -= _angleVelHorz / 3;
            _angleVert -= _angleVelVert / 3;

            
            //Calculate view
            float4x4 headsetRotationX = float4x4.CreateRotationX(-gameRotationVector[2] + _angleVert);
            float4x4 headsetRotationY = float4x4.CreateRotationY(-gameRotationVector[0] + _angleHorz);
            float4x4 headsetRotationZ = float4x4.CreateRotationZ(-gameRotationVector[1]);

            if (renderStereo){
                //Render Left Eye
                var camTrans = float4x4.CreateTranslation(eyeDistance / 2, -200, 0);
                var mtxCam = float4x4.LookAt(eyeDistance/2, 0, 0, 0, 0, 400,0,1,0);
                RC.ModelView = headsetRotationZ * headsetRotationX * headsetRotationY * mtxCam * camTrans;
                _stereo3d.Prepare(Stereo3DEye.Left);
                _sceneRenderer.Render(RC);
                _stereo3d.Save();

                //Render Right Eye
                camTrans = float4x4.CreateTranslation(-eyeDistance / 2, -200, 0);
                mtxCam = float4x4.LookAt(-eyeDistance / 2, 0, 0, 0, 0, 400, 0, 1, 0);
                RC.ModelView = headsetRotationZ * headsetRotationX * headsetRotationY * mtxCam * camTrans;
                _stereo3d.Prepare(Stereo3DEye.Right);
                _sceneRenderer.Render(RC);
                _stereo3d.Save();

                _stereo3d.Display();
            } else {
                // Render the scene loaded in Init()*/
                var camTrans = float4x4.CreateTranslation(0, -200, 0);
                RC.ModelView = headsetRotationZ * headsetRotationX * headsetRotationY * camTrans ;
                _sceneRenderer.Render(RC);                
            }

             #if GUI_SIMPLE
                _guiHandler.RenderGUI(); //GUI can also be rendered for each eye
             #endif


            // Swap buffers: Show the contents of the backbuffer (containing the currently rerndered farame) on the front buffer.
            Present();
        }

        private InputDevice Creator(IInputDeviceImp device)
        {
            throw new NotImplementedException();
        }

        // Is called when the window was resized
        public override void Resize()
        {
            // Set the new rendering area to the entire new windows size
            RC.Viewport(0, 0, Width, Height);

            // Create a new projection matrix generating undistorted images on the new aspect ratio.
            float aspectRatio;
            if (renderStereo)
            {
                aspectRatio = Width / ((float)Height * 2);
            } else
            {
                aspectRatio = Width / (float)Height;
            }

            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio
            // Front clipping happens at 1 (Objects nearer than 1 world unit get clipped)
            // Back clipping happens at 2000 (Anything further away from the camera than 2000 world units gets clipped, polygons will be cut)
            var projection = float4x4.CreatePerspectiveFieldOfView(pi/2, aspectRatio, 1, 20000);
            RC.Projection = projection;

            #if GUI_SIMPLE
            _guiSubText.PosX = (int)((Width - _subtextWidth) / 2);
            _guiSubText.PosY = (int)(Height - _subtextHeight - 3);

            _guiHandler.Refresh();
            #endif

        }

        #if GUI_SIMPLE
        private void _guiFuseeLink_OnGUIButtonLeave(GUIButton sender, GUIButtonEventArgs mea)
        {
            _guiFuseeLink.ButtonColor = new float4(0, 0, 0, 0);
            _guiFuseeLink.BorderWidth = 0;
            SetCursor(CursorType.Standard);
        }

        private void _guiFuseeLink_OnGUIButtonEnter(GUIButton sender, GUIButtonEventArgs mea)
        {
            _guiFuseeLink.ButtonColor = new float4(0, 0.6f, 0.2f, 0.4f);
            _guiFuseeLink.BorderWidth = 1;
            SetCursor(CursorType.Hand);
        }

        void _guiFuseeLink_OnGUIButtonDown(GUIButton sender, GUIButtonEventArgs mea)
        {
            OpenLink("http://fusee3d.org");
        }
        #endif
    }
}
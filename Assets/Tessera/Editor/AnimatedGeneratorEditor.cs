using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(AnimatedGenerator))]
    class AnimatedGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ag = (AnimatedGenerator)target;
            bool showStart = false;
            bool showStop = false;
            bool showPause = false;
            bool showResume = false;

            switch (ag.State)
            {
                case AnimatedGenerator.AnimatedGeneratorState.Stopped:
                    showStart = true;
                    break;
                case AnimatedGenerator.AnimatedGeneratorState.Initializing:
                    showStop = true;
                    break;
                case AnimatedGenerator.AnimatedGeneratorState.Running:
                    showStop = showPause = true;
                    break;
                case AnimatedGenerator.AnimatedGeneratorState.Paused:
                    showStop = showResume = true;
                    break;
            }

            if(showPause)
            {

                if (GUILayout.Button("Pause"))
                {
                    ag.PauseGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update -= ag.Step;
                    }
                }
            }

            if(showResume)
            {
                if (GUILayout.Button("Resume"))
                {
                    ag.ResumeGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update += ag.Step;
                    }
                }
            }

            if (showStart)
            {
                if (GUILayout.Button("Start"))
                {
                    ag.StartGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update += ag.Step;
                    }
                }
            }

            if (showStop)
            {
                if (GUILayout.Button("Stop"))
                {
                    ag.StopGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update -= ag.Step;
                    }
                }
            }
        }
    }
}

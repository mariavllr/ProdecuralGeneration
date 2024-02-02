using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera;
using System.Linq;
using System;
using UnityEngine.Events;

namespace Tessera
{
    public enum TesseraMultipassPassType
    {
        Generator,
        Event,
    }

    [Serializable]
    public class TesseraMultipassPass
    {
        public TesseraMultipassPassType passType;

        public TesseraGenerator generator;

        public UnityEvent generateEvent;
        public UnityEvent clearEvent;
    }

    public class TesseraMultipassGenerator : MonoBehaviour
    {
        public TesseraMultipassPass[] passes;

        public void Clear()
        {
            foreach (var pass in passes)
            {
                if (pass.passType == TesseraMultipassPassType.Generator)
                {
                    pass.generator.Clear();
                }
                else if (pass.passType == TesseraMultipassPassType.Event)
                {
                    pass.clearEvent?.Invoke();
                }
                else
                {
                    throw new Exception($"Unknown passType {pass.passType}");
                }
            }
        }

        public void Generate()
        {
            foreach(var pass in passes)
            {
                if(pass.passType == TesseraMultipassPassType.Generator)
                {
                    pass.generator.Generate();
                }
                else if (pass.passType == TesseraMultipassPassType.Event)
                {
                    pass.generateEvent?.Invoke();
                }
                else
                {
                    throw new Exception($"Unknown passType {pass.passType}");
                }
            }
        }
    }
}
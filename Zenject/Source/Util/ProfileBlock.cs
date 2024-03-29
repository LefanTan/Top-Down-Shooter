using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ModestTree;

#if !NOT_UNITY3D
using UnityEngine;
using UnityEngine.Profiling;
#endif

namespace Zenject
{
    public class ProfileBlock : IDisposable
    {
#if UNITY_EDITOR
        static bool _isActive;

        bool _rootBlock;

        ProfileBlock(string sampleName, bool rootBlock)
        {
            Profiler.BeginSample(sampleName);
            _rootBlock = rootBlock;

            if (rootBlock)
            {
                Assert.That(!_isActive);
                _isActive = true;
            }
        }

        ProfileBlock(string sampleName)
            : this(sampleName, false)
        {
        }

        public static Regex ProfilePattern
        {
            get;
            set;
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj1, object obj2)
        {
            if (!Profiler.enabled)
            {
                return null;
            }
            return Start(string.Format(sampleNameFormat, obj1, obj2));
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj)
        {
            if (!Profiler.enabled)
            {
                return null;
            }
            return Start(string.Format(sampleNameFormat, obj));
        }

        public static ProfileBlock Start(string sampleName)
        {
            if (!Profiler.enabled)
            {
                return null;
            }

            if (ProfilePattern == null || _isActive)
            // If we are below one of the regex matches, show all all profile blocks
            {
                return new ProfileBlock(sampleName);
            }

            if (ProfilePattern.Match(sampleName).Success)
            {
                return new ProfileBlock(sampleName, true);
            }

            return null;
        }

        public void Dispose()
        {
            Profiler.EndSample();

            if (_rootBlock)
            {
                Assert.That(_isActive);
                _isActive = false;
            }
        }

#else
        ProfileBlock(string sampleName, bool rootBlock)
        {
        }

        ProfileBlock(string sampleName)
            : this(sampleName, false)
        {
        }

        public static Regex ProfilePattern
        {
            get;
            set;
        }

        // Remove the call completely for builds
        public static ProfileBlock Start()
        {
            return null;
        }

        // Remove the call completely for builds
        public static ProfileBlock Start(string sampleNameFormat, object obj1, object obj2)
        {
            return null;
        }

        // Remove the call completely for builds
        public static ProfileBlock Start(string sampleNameFormat, object obj)
        {
            return null;
        }

        // Remove the call completely for builds
        public static ProfileBlock Start(string sampleName)
        {
            return null;
        }

        public void Dispose()
        {
        }
#endif
    }
}

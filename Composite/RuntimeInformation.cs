﻿using System;
using Composite.Core.IO;
using System.Reflection;


namespace Composite
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
	public static class RuntimeInformation
	{
        private static bool _isUnitTestDetermined = false;
        private static bool _isUnitTest = false;
        private static string _uniqueInstanceName = null;
        private static string _uniqueInstanceNameSafe = null;

        /// <exclude />
        public static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }


        /// <exclude />
	    public static bool AppDomainLockingDisabled
	    {
            get
            {
                return IsUnittest;
            }
	    }


        /// <exclude />
        public static bool IsUnittest
        {
            get
            {
                if (_isUnitTestDetermined==false)
                {
                    _isUnitTest = RuntimeInformation.IsUnittestImpl;
                    _isUnitTestDetermined = true;
                }

                return _isUnitTest;
            }
        }


        private static bool IsUnittestImpl
        {
            get
            {
                if (AppDomain.CurrentDomain.SetupInformation.ApplicationName == null)
                {
                    return true;
                }

                if (AppDomain.CurrentDomain.SetupInformation.ApplicationName == "vstesthost.exe")
                {
                    return true;
                }

                return false;
            }
        }



        /// <exclude />
        public static Version ProductVersion
        {
            get
            {
                return typeof(RuntimeInformation).Assembly.GetName().Version;
            }
        }



        /// <exclude />
        public static string ProductTitle
        {
            get
            {
                Assembly asm = typeof(RuntimeInformation).Assembly;
                string assemblyTitle = ((AssemblyTitleAttribute)asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;

                return assemblyTitle;
            }
        }



        /// <exclude />
        public static string UniqueInstanceName
        {
            get
            {
                if (_uniqueInstanceName == null)
                {
                    string baseString = PathUtil.BaseDirectory.ToLowerInvariant();
                    _uniqueInstanceName = string.Format("C1@{0}", PathUtil.CleanFileName(baseString));
                }

                return _uniqueInstanceName;
            }
        }



        /// <exclude />
        public static string UniqueInstanceNameSafe
        {
            get
            {
                if (_uniqueInstanceNameSafe == null)
                {
                    string baseString = PathUtil.BaseDirectory.ToLowerInvariant().Replace(@"\", "-").Replace("/", "-");
                    _uniqueInstanceNameSafe = string.Format("C1@{0}", PathUtil.CleanFileName(baseString));
                }

                return _uniqueInstanceNameSafe;
            }
        }
	}
}

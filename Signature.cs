﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains informations about the assembly.
    /// </summary>
    public static class Signature
    {
        /// <summary>
        /// Gets the version number of the executing assembly.
        /// </summary>
        /// <value>The software version.</value>
        public static string Version { get; internal set; }

        /// <summary>
        /// Gets the date and time when the executing assembly was compiled.
        /// </summary>
        /// <value>The compile time.</value>
        public static DateTime CompileTime { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the executed assembly was compiled from Visual Studio under the Debug configuration.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the DEBUG constant is defined; otherwise, <c>false</c>.
        /// </value>
        public static bool IsDebug
        {
            get
            {
                return
#if DEBUG
                    true
#else
                    false
#endif
                    ;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this assembly is obfuscated.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this assembly is obfuscated; otherwise, <c>false</c>.
        /// </value>
        public static bool IsObfuscated { get; internal set; }

        /// <summary>
        /// Gets the full path to the executing assembly.
        /// </summary>
        /// <value>The full path.</value>
        public static string FullPath { get; internal set; }

        /// <summary>
        /// This number is used for various purposes where a non-random unique number is required.
        /// </summary>
        public static long MagicNumber
        {
            get
            {
                return 0xFEEDFACEC0FFEE;
            }
        }
        
        /// <summary>
        /// Initializes the <see cref="Signature"/> class.
        /// </summary>
        static Signature()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;

            Version      = ver.Major + "." + ver.MajorRevision + "." + ver.Build.ToString("0000") + "." + ver.Revision.ToString("00000");
            CompileTime  = new DateTime(2000, 1, 1, 1, 0, 0).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            IsObfuscated = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(SuppressIldasmAttribute), false).Length == 1;
            try { FullPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar; } catch (ArgumentException) { }
        }

        /// <summary>
        /// Gets the numbers. This is an easter egg. ;)
        /// </summary>
        public static IEnumerable<int> GetNumbers()
        {
            for (var x = 1; x != 6; x++)
            {
                yield return (int)(60 + 4.25 * Math.Pow(x * x, 2) + 91.75 * x * x - 29.375 * x * Math.Pow(x, 2) - 0.22499999 * x * Math.Pow(x, 2) * Math.Pow(x, 2) - 122.4 * x);
            }
        }
    }
}
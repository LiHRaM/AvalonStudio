﻿using AvalonStudio.Extensibility;
using AvalonStudio.Packages;
using AvalonStudio.Platforms;
using AvalonStudio.Projects;
using AvalonStudio.Projects.CPlusPlus;
using AvalonStudio.Projects.Standard;
using AvalonStudio.Toolchains.CustomGCC;
using AvalonStudio.Toolchains.GCC;
using AvalonStudio.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AvalonStudio.Toolchains.PublishedGCC
{
    public class PublishedGCCToolchain : GCCToolchain
    {
        private string _executableExtension;
        private string _staticLibraryExtension;
        private string _binDirectory;

        private PublishedGCCToolchainSettings _settings;
        private GccConfiguration _gccConfig;

        public override string ExecutableExtension => _executableExtension;

        public override string StaticLibraryExtension => _staticLibraryExtension;

        public override Version Version => new Version();

        public override string Description => "Allows developer to specify any GCC compatible toolchain to use.";

        public override string BinDirectory => _binDirectory;

        public override string CCExecutable => _gccConfig?.CC;

        public override string CPPExecutable => _gccConfig?.Cpp;

        public override string ARExecutable => _gccConfig?.AR;

        public override string LDExecutable => _gccConfig?.LD;

        public override string SizeExecutable => _gccConfig?.Size;

        public override string GDBExecutable => _gccConfig?.Gdb;

        //public override string LibraryQueryCommand => Path.Combine(BinDirectory, _settings.LibraryQueryCommand + Platform.ExecutableExtension);

        public override bool CanHandle(IProject project)
        {
            var result = false;

            if (project is CPlusPlusProject)
            {
                result = true;
            }

            return result;
        }

        public override string GetBaseLibraryArguments(IStandardProject superProject)
        {
            var settings = superProject.GetToolchainSettings<GccToolchainSettings>();
            string result = string.Empty;

            // TODO linked libraries won't make it in on nano... Please fix -L directory placement in compile string.
            switch (settings.LinkSettings.Library)
            {
                case LibraryType.NanoCLib:
                    result += "-lm -lc_nano -lsupc++_nano -lstdc++_nano ";
                    break;

                case LibraryType.BaseCLib:
                    result += "-lm -lc -lstdc++ -lsupc++ ";
                    break;

                case LibraryType.SemiHosting:
                    result += "-lm -lgcc -lc -lrdimon ";
                    break;

                case LibraryType.Retarget:
                    result += "-lm -lc -lg -lnosys -lstdc++ -lsupc++ ";
                    break;
            }

            return result;
        }

        public override string GetCompilerArguments(IStandardProject superProject, IStandardProject project, ISourceFile file)
        {
            var result = string.Empty;

            var settings = superProject.GetToolchainSettings<GccToolchainSettings>();

            result += "-Wall -c ";

            if (settings.CompileSettings.DebugInformation)
            {
                result += "-ggdb3 ";
            }

            if (file == null || file.Extension == ".cpp")
            {
                switch (settings.CompileSettings.CppLanguageStandard)
                {
                    case CppLanguageStandard.Cpp98:
                        result += "-std=c++98 ";
                        break;

                    case CppLanguageStandard.Cpp03:
                        result += "-std=c++03 ";
                        break;

                    case CppLanguageStandard.Cpp11:
                        result += "-std=c++11 ";
                        break;

                    case CppLanguageStandard.Cpp14:
                        result += "-std=c++14 ";
                        break;

                    case CppLanguageStandard.Cpp17:
                        result += "-std=c++17 ";
                        break;

                    case CppLanguageStandard.Gnu11:
                        result += "-std=gnu++11 ";
                        break;

                    case CppLanguageStandard.Gnu14:
                        result += "-std=gnu++14 ";
                        break;

                    default:
                        break;
                }

                if (!settings.CompileSettings.Rtti)
                {
                    result += "-fno-rtti ";
                }

                if (!settings.CompileSettings.Exceptions)
                {
                    result += "-fno-exceptions ";
                }
            }

            if (file == null || file.Extension == ".c")
            {
                switch (settings.CompileSettings.CLanguageStandard)
                {
                    case CLanguageStandard.C89:
                        result += "-std=c89 ";
                        break;

                    case CLanguageStandard.C99:
                        result += "-std=c99 ";
                        break;

                    case CLanguageStandard.C11:
                        result += "-std=c11 ";
                        break;
                }
            }

            switch (settings.CompileSettings.Fpu)
            {
                case FPUSupport.Soft:
                    result += "-mfpu=fpv4-sp-d16 -mfloat-abi=softfp ";
                    break;

                case FPUSupport.Hard:
                    result += "-mfpu=fpv4-sp-d16 -mfloat-abi=hard ";
                    break;
            }

            switch (settings.CompileSettings.Fpu)
            {
                case FPUSupport.Soft:
                    {
                        result += "-mfpu=fpv4-sp-d16 -mfloat-abi=softfp ";
                    }
                    break;

                case FPUSupport.Hard:
                    {
                        result += "-mfpu=fpv4-sp-d16 -mfloat-abi=hard ";
                    }
                    break;
            }

            // TODO make this an option.
            result += "-ffunction-sections -fdata-sections ";

            switch (settings.CompileSettings.Optimization)
            {
                case OptimizationLevel.None:
                    {
                        result += "-O0 ";
                    }
                    break;

                case OptimizationLevel.Debug:
                    {
                        result += "-O0 ";
                    }
                    break;

                case OptimizationLevel.Level1:
                    {
                        result += "-O1 ";
                    }
                    break;

                case OptimizationLevel.Level2:
                    {
                        result += "-O2 ";
                    }
                    break;

                case OptimizationLevel.Level3:
                    {
                        result += "-O3 ";
                    }
                    break;

                case OptimizationLevel.Size:
                    {
                        result += "-Os ";
                    }
                    break;

                case OptimizationLevel.Speed:
                    {
                        result += "-Ofast ";
                    }
                    break;
            }

            result += settings.CompileSettings.CustomFlags + " ";

            // Referenced includes
            var referencedIncludes = project.GetReferencedIncludes();

            referencedIncludes.Select(s => result += $"-I\"{ Path.Combine(project.CurrentDirectory, s)}\" ").ToList();

            // global includes
            var globalIncludes = superProject.GetGlobalIncludes();

            globalIncludes.Select(s => result += $"-I\"{s}\" ").ToList();

            // includes
            project.Includes.Select(s => result += $"-I\"{ Path.Combine(project.CurrentDirectory, s.Value)}\" ").ToList();

            var referencedDefines = project.GetReferencedDefines();
            referencedDefines.Select(s => result += $"-D{s} ").ToList();

            var toolchainIncludes = GetToolchainIncludes(file);
            toolchainIncludes.Select(s => result += $"-isystem\"{s}\" ").ToList();

            // global includes
            var globalDefines = superProject.GetGlobalDefines();

            globalDefines.Select(s => result += $"-D{s} ").ToList();

            project.Defines.Select(s => result += $"-D{s.Value} ").ToList();

            superProject.ToolChainArguments.Select(s => result += $" {s}").ToList();

            superProject.CompilerArguments.Select(s => result += $" {s}").ToList();

            // TODO factor out this code from here!
            if (file != null)
            {
                switch (file.Extension)
                {
                    case ".c":
                        {
                            superProject.CCompilerArguments.Select(s => result += $" {s}");
                        }
                        break;

                    case ".cpp":
                        {
                            superProject.CppCompilerArguments.Select(s => result += $" {s}");
                        }
                        break;
                }
            }

            return result;
        }

        public override IList<object> GetConfigurationPages(IProject project)
        {
            return new List<object>
            {
                new PublishedToolchainSettingsViewModel(project),
                new CompileSettingsFormViewModel(project),
                new LinkerSettingsFormViewModel(project)
        };
        }

        private string GetLinkerScriptLocation(IStandardProject project)
        {
            return Path.Combine(project.CurrentDirectory, "link.ld");
        }

        public override string GetLinkerArguments(IStandardProject superProject, IStandardProject project)
        {
            var settings = project.GetToolchainSettings<GccToolchainSettings>();

            if (superProject != null && settings.LinkSettings.UseMemoryLayout && project.Type != ProjectType.StaticLibrary)
            {
                // GenerateLinkerScript(superProject);
            }

            var result = string.Empty;

            result += string.Format("{0} ", settings.LinkSettings.MiscLinkerArguments);

            switch (settings.CompileSettings.Fpu)
            {
                case FPUSupport.Soft:
                    result += " -mfpu=fpv4-sp-d16 -mfloat-abi=softfp ";
                    break;

                case FPUSupport.Hard:
                    result += " -mfpu=fpv4-sp-d16 -mfloat-abi=hard ";
                    break;
            }

            if (settings.LinkSettings.NotUseStandardStartupFiles)
            {
                result += "-nostartfiles ";
            }

            if (settings.LinkSettings.DiscardUnusedSections)
            {
                result += "-Wl,--gc-sections ";
            }

            switch (settings.CompileSettings.Optimization)
            {
                case OptimizationLevel.None:
                    result += " -O0";
                    break;

                case OptimizationLevel.Level1:
                    result += " -O1";
                    break;

                case OptimizationLevel.Level2:
                    result += " -O2";
                    break;

                case OptimizationLevel.Level3:
                    result += " -O3";
                    break;
            }

            if (settings.LinkSettings.UseMemoryLayout)
            {
                result += string.Format(" -L{0} -Wl,-T\"{1}\"", project.CurrentDirectory, GetLinkerScriptLocation(project));
            }

            return result;
        }

        public override async Task<bool> InstallAsync(IConsole console, IProject project)
        {
            bool result = true;

            _settings = project.GetToolchainSettings<PublishedGCCToolchainSettings>();

            if(_settings.Toolchain != null)
            {
                await PackageManager.EnsurePackage(_settings.Toolchain, _settings.Version, IoC.Get<IConsole>(), ignoreRid: true);

                _gccConfig = GccConfigurationsManager.GetConfiguration(_settings.Toolchain, _settings.Version);

                result = await _gccConfig.ResolveAsync();
            }

            if (result)
            {
                result = await base.InstallAsync(console, project);
            }

            return result;
        }
    }
}

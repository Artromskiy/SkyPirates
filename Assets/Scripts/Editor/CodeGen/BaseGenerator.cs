﻿#nullable enable
using System.IO;
using System.Runtime.CompilerServices;
using UnityCodeGen;
using UnityEngine;

namespace DVG.Editor.CodeGen
{
    public abstract class BaseGenerator : ICodeGenerator
    {
        public const string RuntimeCodePath = "Assets/Scripts/Generated/Runtime";
        public const string EditorCodePath = "Assets/Scripts/Generated/Editor";
        private const string GeneratedCodeFileExtension = ".g.cs";

        protected abstract CodePath CodePath { get; }
        protected abstract void Generate(Context ctx);

        public void Execute(GeneratorContext context)
        {
            string codePath = CodePath switch
            {
                CodePath.Runtime => RuntimeCodePath,
                CodePath.Editor => EditorCodePath,
                _ => throw new System.NotImplementedException(),
            };
            Debug.Log($"[CodeGen] {this}");
            context.OverrideFolderPath(codePath);

            Generate(new Context(context));
        }



        public readonly struct Context
        {
            private readonly GeneratorContext ctx;

            public Context(GeneratorContext ctx)
            {
                this.ctx = ctx;
            }

            public readonly void AddCode(string fileName, string code, [CallerFilePath] string? generatorPath = default)
            {
                string fullPath = fileName + GeneratedCodeFileExtension;
                ctx.AddCode(fullPath, GeneratorNote(generatorPath) + code);
            }

            private static string GeneratorNote(string? generatorPath) =>
$@"//------------------------------------------------------------------------------
// <auto-generated>
//
// This code was generated by a tool.
// Path: {Path.GetRelativePath(Application.dataPath, generatorPath)}
//
// Changes to this file may cause incorrect behavior
// and will be lost if the code is regenerated.
// 
// </auto-generated>
//------------------------------------------------------------------------------
";
        }
    }
}
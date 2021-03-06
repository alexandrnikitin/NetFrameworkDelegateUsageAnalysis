﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Analysis
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var folderPath = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\";

            int i = 0;
            foreach (var dll in GetAssemblies(folderPath))
            {
                ModuleDefinition module;
                if (!TryGetModule(dll, out module))
                {
                    continue;
                }

                foreach (var currentType in module.GetTypes())
                {
                    foreach (var currentMethod in currentType.GetMethods())
                    {
                        if (currentMethod.HasBody)
                        {
                            foreach (var currentInstruction in currentMethod.Body.Instructions)
                            {
                                if (currentInstruction.OpCode != OpCodes.Ldftn)
                                {
                                    continue;
                                }

                                var nextInstruction = currentInstruction.Next;
                                if (nextInstruction.OpCode != OpCodes.Newobj)
                                {
                                    continue;
                                }

                                var possibleDelegateCtr = (MethodReference)nextInstruction.Operand;
                                var isConstructor = false;
                                try
                                {
                                    isConstructor = possibleDelegateCtr.Resolve().IsConstructor;
                                }
                                catch
                                {
                                    // todo check the exceptions
                                }

                                if (!isConstructor)
                                {
                                    continue;
                                }

                                var methodPointer = ((MethodReference)currentInstruction.Operand).Resolve();

                                if (
                                    IsEmpty(methodPointer)/* || 
                                    IsIdentity(methodPointer) ||
                                    IsAlwaysTrue(methodPointer) || 
                                    IsCompatible(methodPointer) || 
                                    IsCast(methodPointer)*/
                                    )
                                {
                                    i++;

                                    Console.WriteLine("--------------------------------------------------------");
                                    Console.WriteLine("Item #{0}", i);
                                    Console.WriteLine("Type name: {0}", currentType);
                                    Console.WriteLine("Method full name: {0}", currentMethod.FullName);
                                    Console.WriteLine("Instructions:");
                                    Console.WriteLine(currentInstruction);
                                    Console.WriteLine(nextInstruction);
                                    Console.WriteLine();
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Total items found: {0}", i);
        }

        private static IEnumerable<string> GetAssemblies(string folderPath)
        {
            return Directory.EnumerateFiles(folderPath, "*.dll");
        }

        private static bool IsAlwaysTrue(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasBody)
            {
                return false;
            }

            if (methodDefinition.Body.Instructions[0].OpCode != OpCodes.Ldc_I4_1 ||
                methodDefinition.Body.Instructions[1].OpCode != OpCodes.Ret)
            {
                return false;
            }

            return true;
        }

        private static bool IsCast(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasBody)
            {
                return false;
            }

            // todo quite straightforward check, find other ways
            if (methodDefinition.Body.Instructions[0].OpCode != OpCodes.Ldarg_0 ||
                methodDefinition.Body.Instructions[1].OpCode != OpCodes.Box ||
                methodDefinition.Body.Instructions[2].OpCode != OpCodes.Unbox_Any ||
                methodDefinition.Body.Instructions[3].OpCode != OpCodes.Ret)
            {
                return false;
            }

            return true;
        }

        private static bool IsCompatible(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasBody)
            {
                return false;
            }

            // todo quite straightforward check, find other ways
            if (methodDefinition.Body.Instructions[0].OpCode != OpCodes.Ldarg_0 ||
                methodDefinition.Body.Instructions[1].OpCode != OpCodes.Box ||
                methodDefinition.Body.Instructions[2].OpCode != OpCodes.Isinst ||
                methodDefinition.Body.Instructions[3].OpCode != OpCodes.Ldnull ||
                methodDefinition.Body.Instructions[4].OpCode != OpCodes.Cgt_Un ||
                methodDefinition.Body.Instructions[5].OpCode != OpCodes.Ret)
            {
                return false;
            }

            return true;
        }

        private static bool IsEmpty(MethodDefinition methodDefinition)
        {
            if (methodDefinition.HasBody && methodDefinition.Body.Instructions.Count == 1)
            {
                if (methodDefinition.Body.Instructions.Single().OpCode == OpCodes.Ret)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIdentity(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasBody)
            {
                return false;
            }

            if (methodDefinition.Body.Instructions[0].OpCode != OpCodes.Ldarg_0 ||
                methodDefinition.Body.Instructions[1].OpCode != OpCodes.Ret)
            {
                return false;
            }

            return true;
        }

        private static bool TryGetModule(string pathToAssembly, out ModuleDefinition moduleDefinition)
        {
            try
            {
                moduleDefinition = ModuleDefinition.ReadModule(pathToAssembly);
                return true;
            }
            catch
            {
                moduleDefinition = default(ModuleDefinition);
                return false;
            }
        }
    }
}
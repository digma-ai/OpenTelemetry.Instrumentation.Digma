using System.Reflection;
using AutoInstrumentation.UnitTests.TypesToTest;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace AutoInstrumentation.UnitTests
{
    [TestClass]
    public class MethodDiscoveryTests
    {
        private static Assembly ThisAssembly => typeof(MethodDiscoveryTests).Assembly;
    
        [TestMethod]
        public void EmptyConfiguration_NothingToPath()
        {
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, new Configuration());
            methods.Should().BeEmpty();
        }

        [TestMethod]
        public void Patch_By_NamespaceSimplePattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Contain(x => x.DeclaringType == typeof(Record));
            methods.Should().Contain(x => x.DeclaringType == typeof(Class));
        }
        
        [TestMethod]
        public void Patch_By_NamespaceRegexPattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "/^AutoInstrumentation.*TypesToTest/"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Contain(x => x.DeclaringType == typeof(Record));
            methods.Should().Contain(x => x.DeclaringType == typeof(Class));
        }
        
        [TestMethod]
        public void Patch_By_ClassesSimplePattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        Classes = "Rec*"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Contain(x => x.DeclaringType == typeof(Record));
            methods.Should().NotContain(x => x.DeclaringType == typeof(Class));
        }
        
        [TestMethod]
        public void Patch_By_ClassesRegexPattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        Classes = "/Rec.*/"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Contain(x => x.DeclaringType == typeof(Record));
            methods.Should().NotContain(x => x.DeclaringType == typeof(Class));
        }
        
        [TestMethod]
        public void Patch_By_MethodsSimplePattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        Classes = "Rec*",
                        Methods = "Private*"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethodAsync");
        }    
        
        [TestMethod]
        public void Patch_By_MethodsRegexPattern()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        Classes = "Rec*",
                        Methods = "/Private.*/"
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethodAsync");
        }
        
        [TestMethod]
        public void Patch_By_PublicAccessModifier()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        AccessModifier = MethodAccessModifier.Public
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PublicMethod",
                x => x.DeclaringType == typeof(Record) && x.Name == "PublicMethodAsync",
                x => x.DeclaringType == typeof(Class) && x.Name == "PublicMethod",
                x => x.DeclaringType == typeof(Class) && x.Name == "PublicMethodAsync");
        }     
        
        [TestMethod]
        public void Patch_By_PrivateAccessModifier()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        AccessModifier = MethodAccessModifier.Private
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethodAsync",
                x => x.DeclaringType == typeof(Class) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Class) && x.Name == "PrivateMethodAsync");
        }    
        
        [TestMethod]
        public void Patch_By_SyncAccessModifier()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        SyncModifier = MethodSyncModifier.Sync
                    }
                ]
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Record) && x.Name == "PublicMethod",
                x => x.DeclaringType == typeof(Class) && x.Name == "PrivateMethod",
                x => x.DeclaringType == typeof(Class) && x.Name == "PublicMethod");
        }

        [TestMethod]
        public void ExcludeMethods()
        {
            var configuration = new Configuration
            {
                Include =
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        SyncModifier = MethodSyncModifier.Sync
                    }
                ],
                Exclude = 
                [
                    new InstrumentationRule
                    {
                        Namespaces = "AutoInstrumentation.UnitTests.TypesTo*",
                        AccessModifier = MethodAccessModifier.Private
                    }
                ],
            };
            var methods = MethodDiscovery.GetMethodsToPatch(ThisAssembly, configuration);
            methods.Should().Satisfy(
                x => x.DeclaringType == typeof(Record) && x.Name == "PublicMethod",
                x => x.DeclaringType == typeof(Class) && x.Name == "PublicMethod");
        }
    }
}

namespace AutoInstrumentation.UnitTests.TypesToTest
{
    public record Record
    {
        private void PrivateMethod()
        {
        }

        public void PublicMethod()
        {
        }

        private async Task PrivateMethodAsync()
        {
        }

        public async Task PublicMethodAsync()
        {
        }
    }    
    
    public class Class
    {
        private void PrivateMethod()
        {
        }

        public void PublicMethod()
        {
        }

        private async Task PrivateMethodAsync()
        {
        }

        public async Task PublicMethodAsync()
        {
        }
    }

    public abstract class AbsClass
    {
        public void Func(){}
        public abstract void Func2();
    }
}
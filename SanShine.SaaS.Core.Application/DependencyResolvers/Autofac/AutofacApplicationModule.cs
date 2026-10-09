using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.ExceptionHandling;
using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.Validation;
using SanShine.SaaS.Core.Common.Core.Utilities.Interceptors;
using Module = Autofac.Module;

namespace SanShine.SaaS.Core.Application.DependencyResolvers.Autofac;

public class AutofacApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(assembly)
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors(new ProxyGenerationOptions()
            {
                Selector = new AspectInterceptorSelector()
            })
            .SingleInstance();


        builder.RegisterType<ValidationAspect>().SingleInstance();

        builder.RegisterType<DatabaseExceptionHandlingAspect>().SingleInstance();
    }
}

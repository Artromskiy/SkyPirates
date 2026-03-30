using DVG.Core;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DVG.SkyPirates.Client.DI
{
    public static class DIExtensions
    {
        public static void RegisterAndInjectViewModels(this Container container)
        {
            Trace.TraceInformation("[DI] Searching Views");
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var views = all.OfType<IView>().ToArray();
            Trace.TraceInformation($"[DI] Found Views:\n{string.Join('\n', views.Select(v => v.GetType().GetFormattedName()))}");

            Trace.TraceInformation("[DI] Registering Views");
            foreach (var item in views)
                container.RegisterView(item);

            Trace.TraceInformation("[DI] Injecting ViewModels");
            foreach (var item in views)
                container.InjectVM(item);

            Trace.TraceInformation("[DI] Injecting Attributed");
            foreach (var item in all)
                container.InjectAttributed(item);

            Trace.TraceInformation("[DI] Container Analyze");
            container.Analyze();
        }

        public static void RegisterView(this Container container, IView view)
        {
            var interfaces = view.GetType().GetInterfaces();
            var iviewType = typeof(IView);
            foreach (var interf in interfaces)
            {
                if (interf != iviewType && iviewType.IsAssignableFrom(interf))
                {
                    Trace.TraceInformation($"[DI] {interf.GetFormattedName()} registration");
                    container.RegisterInstance(interf, view);
                    Trace.TraceInformation($"[DI] {interf.GetFormattedName()} registered");
                }
            }
        }

        public static void InjectVM(this Container container, IView view)
        {
            var viewType = view.GetType();
            Trace.TraceInformation($"[DI] {viewType.GetFormattedName()} injection");
            var vmProp = viewType.GetProperty("ViewModel");
            var injectMethod = vmProp?.SetMethod;
            if (injectMethod == null)
                return;

            var vmPropType = vmProp.PropertyType;
            Trace.TraceInformation($"[DI] GetInstance: {vmPropType.GetFormattedName()}");
            var vmInstance = container.GetInstance(vmPropType);
            injectMethod.Invoke(view, new object[] { vmInstance });
            Trace.TraceInformation($"[DI] {viewType.GetFormattedName()} registered");
        }

        public static void InjectAttributed(this Container container, object obj)
        {
            var type = obj.GetType();

            while (type?.GetCustomAttribute<InjectAttribute>() != null)
            {
                Trace.TraceInformation($"[DI] {type.GetFormattedName()} injecting");
                var fields = type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectAttribute>() is null)
                        continue;

                    field.SetValue(obj, container.GetInstance(field.FieldType));
                }

                Trace.TraceInformation($"[DI] {type.GetFormattedName()} injected");
                type = type.BaseType;
            }
        }

        public static void Analyze(this Container container)
        {
            try
            {
                container.Verify(VerificationOption.VerifyOnly);
            }
            catch (Exception e)
            {
                Trace.Fail(e.Message, e.StackTrace);
            }

            foreach (var item in Analyzer.Analyze(container))
            {
                Trace.TraceInformation(item.Description);
            }
        }
    }
}

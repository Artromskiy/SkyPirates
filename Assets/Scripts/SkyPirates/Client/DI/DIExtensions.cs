using DVG.Core;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DVG.SkyPirates.Client.DI
{
    public static class DIExtensions
    {
        public static void RegisterAndInjectViewModels(this Container container)
        {
            DVG.Trace.Info("[DI] Searching Views");
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var views = all.OfType<IView>().ToArray();
            DVG.Trace.Info($"[DI] Found Views:\n{string.Join('\n', views.Select(v => v.GetType().GetFormattedName()))}");

            DVG.Trace.Info("[DI] Registering Views");
            foreach (var item in views)
                container.RegisterView(item);

            DVG.Trace.Info("[DI] Injecting ViewModels");
            foreach (var item in views)
                container.InjectVM(item);

            DVG.Trace.Info("[DI] Injecting Attributed");
            foreach (var item in all)
                container.InjectAttributed(item);

            DVG.Trace.Info("[DI] Container Analyze");
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
                    DVG.Trace.Info($"[DI] {interf.GetFormattedName()} registration");
                    container.RegisterInstance(interf, view);
                    DVG.Trace.Info($"[DI] {interf.GetFormattedName()} registered");
                }
            }
        }

        public static void InjectVM(this Container container, IView view)
        {
            var viewType = view.GetType();
            DVG.Trace.Info($"[DI] {viewType.GetFormattedName()} injection");
            var vmProp = viewType.GetProperty("ViewModel");
            var injectMethod = vmProp?.SetMethod;
            if (injectMethod == null)
                return;

            var vmPropType = vmProp.PropertyType;
            DVG.Trace.Info($"[DI] GetInstance: {vmPropType.GetFormattedName()}");
            var vmInstance = container.GetInstance(vmPropType);
            injectMethod.Invoke(view, new object[] { vmInstance });
            DVG.Trace.Info($"[DI] {viewType.GetFormattedName()} registered");
        }

        public static void InjectAttributed(this Container container, object obj)
        {
            var type = obj.GetType();

            while (type?.GetCustomAttribute<InjectAttribute>() != null)
            {
                DVG.Trace.Info($"[DI] {type.GetFormattedName()} injecting");
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

                DVG.Trace.Info($"[DI] {type.GetFormattedName()} injected");
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
                DVG.Debug.Error(e);
            }

            foreach (var item in Analyzer.Analyze(container))
            {
                DVG.Trace.Info(item.Description);
            }
        }
    }
}

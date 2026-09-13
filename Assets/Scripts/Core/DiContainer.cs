using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Minimal reflection-based DI container. Bind an interface to a concrete singleton
    /// instance, then call Inject(target) to populate [Inject]-marked fields/properties
    /// on any object (including MonoBehaviours) during their Awake phase.
    /// </summary>
    public class DiContainer
    {
        private readonly Dictionary<Type, object> _bindings = new Dictionary<Type, object>();

        public BindingHandle<TInterface> Bind<TInterface>()
        {
            return new BindingHandle<TInterface>(this);
        }

        internal void Register<TInterface>(object instance)
        {
            _bindings[typeof(TInterface)] = instance;
        }

        public TInterface Resolve<TInterface>()
        {
            if (_bindings.TryGetValue(typeof(TInterface), out var instance))
            {
                return (TInterface)instance;
            }

            throw new InvalidOperationException($"No binding registered for {typeof(TInterface).FullName}");
        }

        public bool TryResolve(Type interfaceType, out object instance)
        {
            return _bindings.TryGetValue(interfaceType, out instance);
        }

        public void Inject(object target)
        {
            if (target == null) return;

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                if (!Attribute.IsDefined(field, typeof(InjectAttribute))) continue;

                if (TryResolve(field.FieldType, out var dependency))
                {
                    field.SetValue(target, dependency);
                }
                else
                {
                    Debug.LogError($"[DiContainer] Unable to resolve {field.FieldType.Name} for {type.Name}.{field.Name}");
                }
            }

            foreach (var property in type.GetProperties(flags))
            {
                if (!Attribute.IsDefined(property, typeof(InjectAttribute))) continue;
                if (!property.CanWrite) continue;

                if (TryResolve(property.PropertyType, out var dependency))
                {
                    property.SetValue(target, dependency);
                }
                else
                {
                    Debug.LogError($"[DiContainer] Unable to resolve {property.PropertyType.Name} for {type.Name}.{property.Name}");
                }
            }
        }

        public readonly struct BindingHandle<TInterface>
        {
            private readonly DiContainer _container;

            public BindingHandle(DiContainer container)
            {
                _container = container;
            }

            public DiContainer To<TImplementation>() where TImplementation : TInterface, new()
            {
                _container.Register<TInterface>(new TImplementation());
                return _container;
            }

            public DiContainer ToInstance(TInterface instance)
            {
                _container.Register<TInterface>(instance);
                return _container;
            }
        }
    }
}

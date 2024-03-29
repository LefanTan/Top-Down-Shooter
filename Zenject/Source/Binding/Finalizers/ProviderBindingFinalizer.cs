using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;

namespace Zenject
{
    public abstract class ProviderBindingFinalizer : IBindingFinalizer
    {
        public ProviderBindingFinalizer(BindInfo bindInfo)
        {
            BindInfo = bindInfo;
        }

        public bool CopyIntoAllSubContainers
        {
            get { return BindInfo.CopyIntoAllSubContainers; }
        }

        protected BindInfo BindInfo
        {
            get;
            private set;
        }

        protected ScopeTypes GetScope()
        {
            if (BindInfo.Scope == ScopeTypes.Unset)
            {
                Assert.That(!BindInfo.RequireExplicitScope,
                    "Scope must be set for the given binding {0}!  Please either specify AsTransient, AsCached, or AsSingle.", BindInfo.ContextInfo ?? "");
                return ScopeTypes.Transient;
            }

            return BindInfo.Scope;
        }

        public void FinalizeBinding(DiContainer container)
        {
            if (BindInfo.ContractTypes.IsEmpty())
            {
                // We could assert her instead but it is nice when used with things like
                // BindInterfaces() (and there aren't any interfaces) to allow
                // interfaces to be added later
                return;
            }

            OnFinalizeBinding(container);

            if (BindInfo.NonLazy)
            {
                // Note that we can't simply use container.BindRootResolve here because
                // binding finalizers must only use RegisterProvider to allow cloning / bind
                // inheritance to work properly
                var bindingId = new BindingId(
                    typeof(object), DiContainer.DependencyRootIdentifier);

                foreach (var contractType in BindInfo.ContractTypes)
                {
                    container.RegisterProvider(
                        bindingId, null, new ResolveProvider(
                            contractType, container, BindInfo.Identifier, false));
                }
            }
        }

        protected abstract void OnFinalizeBinding(DiContainer container);

        protected void RegisterProvider<TContract>(
            DiContainer container, IProvider provider)
        {
            RegisterProvider(container, typeof(TContract), provider);
        }

        protected void RegisterProvider(
            DiContainer container, Type contractType, IProvider provider)
        {
            container.RegisterProvider(
                new BindingId(contractType, BindInfo.Identifier),
                BindInfo.Condition,
                provider);

            if (contractType.IsValueType())
            {
                var nullableType = typeof(Nullable<>).MakeGenericType(contractType);

                // Also bind to nullable primitives
                // this is useful so that we can have optional primitive dependencies
                container.RegisterProvider(
                    new BindingId(nullableType, BindInfo.Identifier),
                    BindInfo.Condition,
                    provider);
            }
        }

        protected void RegisterProviderPerContract(
            DiContainer container, Func<DiContainer, Type, IProvider> providerFunc)
        {
            foreach (var contractType in BindInfo.ContractTypes)
            {
                RegisterProvider(container, contractType, providerFunc(container, contractType));
            }
        }

        protected void RegisterProviderForAllContracts(
            DiContainer container, IProvider provider)
        {
            foreach (var contractType in BindInfo.ContractTypes)
            {
                RegisterProvider(container, contractType, provider);
            }
        }

        protected void RegisterProvidersPerContractAndConcreteType(
            DiContainer container,
            List<Type> concreteTypes,
            Func<Type, Type, IProvider> providerFunc)
        {
            Assert.That(!BindInfo.ContractTypes.IsEmpty());
            Assert.That(!concreteTypes.IsEmpty());

            foreach (var contractType in BindInfo.ContractTypes)
            {
                foreach (var concreteType in concreteTypes)
                {
                    if (ValidateBindTypes(concreteType, contractType))
                    {
                        RegisterProvider(
                            container, contractType, providerFunc(contractType, concreteType));
                    }
                }
            }
        }

        // Returns true if the bind should continue, false to skip
        bool ValidateBindTypes(Type concreteType, Type contractType)
        {
            if (concreteType.DerivesFromOrEqual(contractType))
            {
                return true;
            }

            if (BindInfo.InvalidBindResponse == InvalidBindResponses.Assert)
            {
                throw Assert.CreateException(
                    "Expected type '{0}' to derive from or be equal to '{1}'", concreteType, contractType);
            }

            Assert.IsEqual(BindInfo.InvalidBindResponse, InvalidBindResponses.Skip);
            return false;
        }

        // Note that if multiple contract types are provided per concrete type,
        // it will re-use the same provider for each contract type
        // (each concrete type will have its own provider though)
        protected void RegisterProvidersForAllContractsPerConcreteType(
            DiContainer container,
            List<Type> concreteTypes,
            Func<DiContainer, Type, IProvider> providerFunc)
        {
            Assert.That(!BindInfo.ContractTypes.IsEmpty());
            Assert.That(!concreteTypes.IsEmpty());

            var providerMap = concreteTypes.ToDictionary(x => x, x => providerFunc(container, x));

            foreach (var contractType in BindInfo.ContractTypes)
            {
                foreach (var concreteType in concreteTypes)
                {
                    if (ValidateBindTypes(concreteType, contractType))
                    {
                        RegisterProvider(container, contractType, providerMap[concreteType]);
                    }
                }
            }
        }
    }
}


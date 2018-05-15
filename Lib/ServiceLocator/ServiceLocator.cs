using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Threading;
using CommonServiceLocator;
using Visyn.Exceptions;
using Visyn.Io;
using Visyn.Wpf.Console.ViewModel;
using Visyn.Build.ViewModel;

namespace Visyn.Wpf.Console.ServiceLocator
{
    public class ServiceLocator : IServiceLocator
    {
        #region Public Properties
        public bool IsDataSource {get;set;} = true;
        public ConsoleWithSeverityViewModel ConsoleWithSeverityViewModel => GetInstance<ConsoleWithSeverityViewModel>();

        public SeverityLevelColorConverter SeverityLevelColorConverter => GetInstance<SeverityLevelColorConverter>();

        #endregion Public Properties

        protected static ConcurrentDictionary<Type, object> SingletonDictionary { get; } = new ConcurrentDictionary<Type, object>(
                                        new [] { new KeyValuePair<Type,object>(typeof(Dispatcher),Dispatcher.CurrentDispatcher)});

        public static bool Register<TService>(TService instance) => SingletonDictionary.TryAdd(typeof(TService), instance);

        public virtual TService GetInstance<TService>() => (TService)GetInstance(typeof(TService));

        public virtual object GetInstance(Type serviceType)
        {
            if (serviceType == typeof(IExceptionHandler)) serviceType = typeof(ConsoleWithSeverityViewModel);
            if (serviceType == typeof(IOutputDevice)) serviceType = typeof(ConsoleWithSeverityViewModel);

            object instance;
            if (SingletonDictionary.TryGetValue(serviceType, out instance)) return instance;

            instance = CreateInstance(serviceType);

            SingletonDictionary.TryAdd(serviceType, instance);
            return instance;
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="serviceType">Type of the specified service.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException">CreateInstance</exception>
        protected virtual object CreateInstance(Type serviceType)
        {
            if (serviceType == typeof(ConsoleWithSeverityViewModel)) return new ConsoleWithSeverityViewModel(10000, GetInstance<Dispatcher>());
            if (serviceType == typeof(SeverityLevelColorConverter)) return new SeverityLevelColorConverter();

            throw new NotImplementedException($"{nameof(CreateInstance)} not implemented for type {serviceType.GetType()}!");
        }

        public virtual IEnumerable<object> GetAllInstances(Type serviceType) => throw new NotImplementedException();

        public virtual IEnumerable<TService> GetAllInstances<TService>() => throw new NotImplementedException();
        public object GetInstance(Type serviceType, string key) => throw new NotImplementedException();
        public TService GetInstance<TService>(string key) => throw new NotImplementedException();

        public object GetService(Type serviceType) => throw new NotImplementedException();

    }
}

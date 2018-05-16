#region Copyright (c) 2015-2018 Visyn
// The MIT License(MIT)
// 
// Copyright (c) 2015-2018 Visyn
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

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

        public static bool Register<TService>(TService instance, bool overwrite)
        {
            if(SingletonDictionary.ContainsKey(typeof(TService)))
            {
                if (overwrite) return SingletonDictionary.TryUpdate(typeof(TService), instance, null);
                throw new Exception($"{nameof(ServiceLocator)}.{nameof(Register)}({typeof(TService).Name}) type already present in dictionary!");
            }
            return SingletonDictionary.TryAdd(typeof(TService), instance);
        }
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

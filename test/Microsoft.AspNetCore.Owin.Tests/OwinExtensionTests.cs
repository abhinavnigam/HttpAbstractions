// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using CreateMiddleware = Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >;
    using AddMiddleware = Action<Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >>;

    public class OwinExtensionTests
    {
        static AppFunc notFound = env => new Task(() => { env["owin.ResponseStatusCode"] = 404; });

        [Fact]
        public void OwinConfigureServiceProviderAddsServices()
        {
            var list = new List<CreateMiddleware>();
            AddMiddleware build = list.Add;
            IServiceProvider serviceProvider = null;
            FakeService fakeService = null;

            var builder = build.UseBuilder(applicationBuilder =>
            {
                serviceProvider = applicationBuilder.ApplicationServices;
                applicationBuilder.Run(context =>
                {
                    fakeService = context.RequestServices.GetService<FakeService>();
                    return Task.FromResult(0);
                });
            },
            new ServiceCollection().AddSingleton(new FakeService()).BuildServiceProvider());

            list.Reverse();
            list.Aggregate(notFound, (next, middleware) => middleware(next)).Invoke(new Dictionary<string, object>());

            Assert.NotNull(serviceProvider);
            Assert.NotNull(serviceProvider.GetService<FakeService>());
            Assert.NotNull(fakeService);
        }

        [Fact]
        public void OwinDefaultNoServices()
        {
            var list = new List<CreateMiddleware>();
            AddMiddleware build = list.Add;
            IServiceProvider expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
            IServiceProvider serviceProvider = null;
            FakeService fakeService = null;
            bool builderExecuted = false;
            bool applicationExecuted = false;

            var builder = build.UseBuilder(applicationBuilder =>
            {
                builderExecuted = true;
                serviceProvider = applicationBuilder.ApplicationServices;
                applicationBuilder.Run(context =>
                {
                    applicationExecuted = true;
                    fakeService = context.RequestServices.GetService<FakeService>();
                    return Task.FromResult(0);
                });
            },
            expectedServiceProvider);

            list.Reverse();
            list.Aggregate(notFound, (next, middleware) => middleware(next)).Invoke(new Dictionary<string, object>());

            Assert.True(builderExecuted);
            Assert.Equal(expectedServiceProvider, serviceProvider);
            Assert.True(applicationExecuted);
            Assert.Null(fakeService);
        }

        private class FakeService
        {
        }
    }
}

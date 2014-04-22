// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Xunit;

namespace Microsoft.Data.SqlServer.FunctionalTests
{
    public class SqlServerConfigPatternsTest
    {
        public class ImplicitServicesAndConfig
        {
            [Fact]
            public async Task Can_query_with_implicit_services_and_OnConfiguring()
            {
                using (await TestDatabase.Northwind())
                {
                    using (var context = new NorthwindContext())
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                public EntitySet<Customer> Customers { get; set; }

                protected override void OnConfiguring(EntityConfigurationBuilder builder)
                {
                    builder.SqlServerConnectionString(TestDatabase.NorthwindConnectionString);
                }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ImplicitServicesExplicitConfig
        {
            [Fact]
            public async Task Can_query_with_implicit_services_and_explicit_config()
            {
                using (await TestDatabase.Northwind())
                {
                    var configuration = new EntityConfigurationBuilder()
                        .SqlServerConnectionString(TestDatabase.NorthwindConnectionString)
                        .BuildConfiguration();

                    using (var context = new NorthwindContext(configuration))
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(EntityConfiguration configuration)
                    : base(configuration)
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ExplicitServicesImplicitConfig
        {
            [Fact]
            public async Task Can_query_with_explicit_services_and_OnConfiguring()
            {
                using (await TestDatabase.Northwind())
                {
                    var serviceProvider = new ServiceCollection()
                        .AddEntityFramework(s => s.AddSqlServer())
                        .BuildServiceProvider();

                    using (var context = new NorthwindContext(serviceProvider))
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider)
                    : base(serviceProvider)
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnConfiguring(EntityConfigurationBuilder builder)
                {
                    builder.SqlServerConnectionString(TestDatabase.NorthwindConnectionString);
                }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ExplicitServicesAndConfig
        {
            [Fact]
            public async Task Can_query_with_explicit_services_and_explicit_config()
            {
                using (await TestDatabase.Northwind())
                {
                    var serviceProvider = new ServiceCollection()
                        .AddEntityFramework(s => s.AddSqlServer())
                        .BuildServiceProvider();

                    var configuration = new EntityConfigurationBuilder()
                        .SqlServerConnectionString(TestDatabase.NorthwindConnectionString)
                        .BuildConfiguration();

                    using (var context = new NorthwindContext(serviceProvider, configuration))
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider, EntityConfiguration configuration)
                    : base(serviceProvider, configuration)
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ExplicitServicesAndNoConfig
        {
            [Fact]
            public async Task Throws_on_attempt_to_use_SQL_Server_without_providing_connection_string()
            {
                using (await TestDatabase.Northwind())
                {
                    var serviceProvider = new ServiceCollection()
                        .AddEntityFramework(s => s.AddSqlServer())
                        .BuildServiceProvider();

                    Assert.Equal(
                        GetString("FormatNoDataStoreConfigured"),
                        Assert.Throws<InvalidOperationException>(() =>
                            {
                                using (var context = new NorthwindContext(serviceProvider))
                                {
                                    Assert.Equal(91, context.Customers.Count());
                                }
                            }).Message);
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider)
                    : base(serviceProvider)
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class NoServicesAndNoConfig
        {
            [Fact]
            public async Task Throws_on_attempt_to_use_context_with_no_store()
            {
                using (await TestDatabase.Northwind())
                {
                    Assert.Equal(
                        GetString("FormatNoDataStoreConfigured"),
                        Assert.Throws<InvalidOperationException>(() =>
                            {
                                using (var context = new NorthwindContext())
                                {
                                    Assert.Equal(91, context.Customers.Count());
                                }
                            }).Message);
                }
            }

            private class NorthwindContext : EntityContext
            {
                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ImplicitConfigButNoServices
        {
            [Fact]
            public async Task Throws_on_attempt_to_use_store_with_no_store_services()
            {
                using (await TestDatabase.Northwind())
                {
                    var serviceProvider = new ServiceCollection()
                        .AddEntityFramework()
                        .BuildServiceProvider();

                    Assert.Equal(
                        GetString("FormatNoDataStoreService"),
                        Assert.Throws<InvalidOperationException>(() =>
                            {
                                using (var context = new NorthwindContext(serviceProvider))
                                {
                                    Assert.Equal(91, context.Customers.Count());
                                }
                            }).Message);
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider)
                    : base(serviceProvider)
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnConfiguring(EntityConfigurationBuilder builder)
                {
                    builder.SqlServerConnectionString(TestDatabase.NorthwindConnectionString);
                }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class InjectContext
        {
            [Fact]
            public async Task Can_register_context_with_DI_container_and_have_it_injected()
            {
                var serviceProvider = new ServiceCollection()
                    .AddEntityFramework(s => s.AddSqlServer())
                    .AddTransient<NorthwindContext, NorthwindContext>()
                    .AddTransient<MyController, MyController>()
                    .BuildServiceProvider();

                using (await TestDatabase.Northwind())
                {
                    await serviceProvider.GetService<MyController>().TestAsync();
                }
            }

            private class MyController
            {
                private readonly NorthwindContext _context;

                public MyController(NorthwindContext context)
                {
                    Assert.NotNull(context);

                    _context = context;
                }

                public async Task TestAsync()
                {
                    Assert.Equal(91, await _context.Customers.CountAsync());
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider)
                    : base(serviceProvider)
                {
                    Assert.NotNull(serviceProvider);
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnConfiguring(EntityConfigurationBuilder builder)
                {
                    builder.SqlServerConnectionString(TestDatabase.NorthwindConnectionString);
                }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class InjectContextAndConfiguration
        {
            [Fact]
            public async Task Can_register_context_and_configuration_with_DI_container_and_have_both_injected()
            {
                var configuration = new EntityConfigurationBuilder()
                    .SqlServerConnectionString(TestDatabase.NorthwindConnectionString)
                    .BuildConfiguration();

                var serviceProvider = new ServiceCollection()
                    .AddEntityFramework(s => s.AddSqlServer())
                    .AddTransient<NorthwindContext, NorthwindContext>()
                    .AddTransient<MyController, MyController>()
                    .AddInstance<EntityConfiguration>(configuration)
                    .BuildServiceProvider();

                using (await TestDatabase.Northwind())
                {
                    await serviceProvider.GetService<MyController>().TestAsync();
                }
            }

            private class MyController
            {
                private readonly NorthwindContext _context;

                public MyController(NorthwindContext context)
                {
                    Assert.NotNull(context);

                    _context = context;
                }

                public async Task TestAsync()
                {
                    Assert.Equal(91, await _context.Customers.CountAsync());
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(IServiceProvider serviceProvider, EntityConfiguration configuration)
                    : base(serviceProvider, configuration)
                {
                    Assert.NotNull(serviceProvider);
                    Assert.NotNull(configuration);
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class InjectConfiguration
        {
            // This one is a bit strange because the context gets the configuration from the service provider
            // but doesn't get the service provider and so creates a new one for use internally. This works fine
            // although it would be much more common to inject both when using DI explicitly.
            [Fact]
            public async Task Can_register_configuration_with_DI_container_and_have_it_injected()
            {
                var configuration = new EntityConfigurationBuilder()
                    .SqlServerConnectionString(TestDatabase.NorthwindConnectionString)
                    .BuildConfiguration();

                var serviceProvider = new ServiceCollection()
                    .AddEntityFramework(s => s.AddSqlServer())
                    .AddTransient<NorthwindContext, NorthwindContext>()
                    .AddTransient<MyController, MyController>()
                    .AddInstance<EntityConfiguration>(configuration)
                    .BuildServiceProvider();

                using (await TestDatabase.Northwind())
                {
                    await serviceProvider.GetService<MyController>().TestAsync();
                }
            }

            private class MyController
            {
                private readonly NorthwindContext _context;

                public MyController(NorthwindContext context)
                {
                    Assert.NotNull(context);

                    _context = context;
                }

                public async Task TestAsync()
                {
                    Assert.Equal(91, await _context.Customers.CountAsync());
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(EntityConfiguration configuration)
                    : base(configuration)
                {
                    Assert.NotNull(configuration);
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ConstructorArgsToBuilder
        {
            [Fact]
            public async Task Can_pass_connection_string_to_constructor_and_use_in_builder()
            {
                using (await TestDatabase.Northwind())
                {
                    using (var context = new NorthwindContext(TestDatabase.NorthwindConnectionString))
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                public NorthwindContext(string connectionString)
                    : base(new EntityConfigurationBuilder()
                        .SqlServerConnectionString(connectionString)
                        .BuildConfiguration())
                {
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        public class ConstructorArgsToOnConfiguring
        {
            [Fact]
            public async Task Can_pass_connection_string_to_constructor_and_use_in_OnConfiguring()
            {
                using (await TestDatabase.Northwind())
                {
                    using (var context = new NorthwindContext(TestDatabase.NorthwindConnectionString))
                    {
                        Assert.Equal(91, await context.Customers.CountAsync());
                    }
                }
            }

            private class NorthwindContext : EntityContext
            {
                private readonly string _connectionString;

                public NorthwindContext(string connectionString)
                {
                    _connectionString = connectionString;
                }

                public EntitySet<Customer> Customers { get; set; }

                protected override void OnConfiguring(EntityConfigurationBuilder builder)
                {
                    builder.SqlServerConnectionString(_connectionString);
                }

                protected override void OnModelCreating(ModelBuilder builder)
                {
                    ConfigureModel(builder);
                }
            }
        }

        private class Customer
        {
            public string CustomerID { get; set; }
            public string CompanyName { get; set; }
            public string Fax { get; set; }
        }

        private static void ConfigureModel(ModelBuilder builder)
        {
            builder
                .Entity<Customer>()
                .Key(c => c.CustomerID)
                .StorageName("Customers");
        }

        private static string GetString(string stringName)
        {
            var strings = typeof(EntityContext).Assembly.GetType(typeof(EntityContext).Namespace + ".Strings");
            return (string)strings.GetTypeInfo().GetDeclaredMethods(stringName).Single().Invoke(null, null);
        }
    }
}

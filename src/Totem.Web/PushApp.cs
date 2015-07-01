﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using Owin;
using Totem.Http;
using Totem.IO;
using Totem.Runtime;

namespace Totem.Web
{
	/// <summary>
	/// An HTTP-bound push application composed by OWIN and SignalR
	/// </summary>
	public class PushApp : Notion, IWebApp
	{
		private readonly WebAppContext _context;
		private readonly bool _enableDetailedErrors;

		public PushApp(WebAppContext context, bool enableDetailedErrors = false)
		{
			_context = context;
			_enableDetailedErrors = enableDetailedErrors;
		}

		public virtual IDisposable Start()
		{
			var startOptions = GetStartOptions();

			var instance = WebApp.Start(startOptions, Startup);

			if(startOptions.Urls.Count == 1)
			{
				Log.Info("[web] Started push server at {Binding:l}", startOptions.Urls[0]);
			}
			else
			{
				Log.Info("[web] Started push server at {Bindings}", startOptions.Urls);
			}

			return instance;
		}

		protected virtual StartOptions GetStartOptions()
		{
			var options = new StartOptions();

			foreach(var binding in _context.Bindings)
			{
				options.Urls.Add(binding.ToString());
			}

			return options;
		}

		protected virtual void Startup(IAppBuilder builder)
		{
			GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());

			builder.MapHubs(new HubConfiguration
			{
				EnableDetailedErrors = _enableDetailedErrors,
				Resolver = new PushDependencyResolver(_context.Scope)
			});
		}

		private sealed class LoggingPipelineModule : HubPipelineModule, ITaggable
		{
			public LoggingPipelineModule()
			{
				Tags = new Tags();
			}

			public Tags Tags { get; private set; }
			private ILog Log { get { return Notion.Traits.Log.Get(this); } }

			protected override void OnIncomingError(Exception ex, IHubIncomingInvokerContext context)
			{
				Log.Error(ex, "An exception occurred in the SignalR pipeline");

				base.OnIncomingError(ex, context);
			}
		}

		// Adapted from http://code.google.com/p/autofac/source/browse/Core/Source/Autofac.Integration.SignalR/AutofacDependencyResolver.cs

		private sealed class PushDependencyResolver : DefaultDependencyResolver
		{
			private readonly ILifetimeScope _scope;

			internal PushDependencyResolver(ILifetimeScope scope)
			{
				_scope = scope;
			}

			public override object GetService(Type serviceType)
			{
				return _scope.ResolveOptional(serviceType) ?? base.GetService(serviceType);
			}

			public override IEnumerable<object> GetServices(Type serviceType)
			{
				var enumerableServiceType = typeof(IEnumerable<>).MakeGenericType(serviceType);

				var instance = (IEnumerable<object>) _scope.Resolve(enumerableServiceType);

				return instance.Any() ? instance : base.GetServices(serviceType);
			}
		}
	}
}
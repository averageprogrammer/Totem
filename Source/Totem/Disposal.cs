﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Totem
{
	/// <summary>
	/// Provides lambda-based implementations of <see cref="IDisposable"/>
	/// </summary>
	public static class Disposal
	{
		public static readonly IDisposable None = Of(() => {});

		public static IDisposable Of(Action disposal)
		{
			return new Disposer(disposal);
		}

		public static IDisposable OfMany(IEnumerable<IDisposable> items)
		{
      var list = items.ToList();

			return Of(() =>
			{
				foreach(var item in list)
				{
					item.Dispose();
				}
			});
		}

		public static IDisposable OfMany(params IDisposable[] items)
		{
			return OfMany(items as IEnumerable<IDisposable>);
		}

		private sealed class Disposer : IDisposable
		{
			private readonly Action _disposal;
			private bool _disposed;

			internal Disposer(Action disposal)
			{
				_disposal = disposal;
			}

			public void Dispose()
			{
				if(!_disposed)
				{
					_disposal();

					_disposed = true;
				}
			}
		}
	}
}
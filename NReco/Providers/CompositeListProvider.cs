using System;
using System.Collections;
using System.Collections.Generic;

namespace NReco.Providers
{
	/// <summary>
	/// Composite list provider implementation.
	/// </summary>
	public class CompositeListProvider<Context,T> : IProvider<Context,IList<T>>
	{
		IProvider[] _Providers;
		bool _IgnoreNullResult = true;
		bool _SkipInvalidEntryType = false;
		

		public bool IgnoreNullResult {
			get { return _IgnoreNullResult; }
			set { _IgnoreNullResult = value; }
		}

		public bool SkipInvalidEntryType {
			get { return _SkipInvalidEntryType; }
			set { _SkipInvalidEntryType = value; }
		}

		public IProvider[] Providers {
			get { return _Providers; }
			set { _Providers = value; }
		}

		public CompositeListProvider()
		{
		}

		protected bool IsEnumerable(object o) {
			return o is IEnumerable && !(o is string);
		}

		public IList<T> GetList(Context context) {
			IList<T> result = new List<T>();
			for (int i=0; i<Providers.Length; i++) {
				object subList = Providers[i].Get(context);
				if (subList==null && IgnoreNullResult)
					continue;
				if (subList==null)
					result.Add( (T)subList );
				else if (subList is T)
					result.Add((T)subList );
				else if (subList is IList<T>) {
					foreach (T entry in (IList<T>)subList)
						result.Add(entry);
				} else if (IsEnumerable(subList)) {
					foreach (object o in (IEnumerable)subList) {
						if (SkipInvalidEntryType) {
							if (o is T)
								result.Add( (T)o );
						} else
							result.Add( (T)o );
					}

				} else {
					if (!SkipInvalidEntryType)
						result.Add( (T)subList );
				}
				
			}
			return result;
		}

		public IList<T> Get(Context context) {
			return GetList(context);
		}

		object IProvider.Get(object context) {
			return Get( (Context)context);
		}

	}
}
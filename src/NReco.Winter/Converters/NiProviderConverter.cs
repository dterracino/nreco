using System;
using System.Collections.Generic;
using System.Text;

using NReco.Converters;
using NI.Common.Providers;

namespace NReco.Winter.Converters {
	
	public class NiProviderConverter : BaseTypeConverter<IObjectProvider,IProvider,NiProviderFromWrapper,NiProviderToWrapper> {

		public NiProviderConverter() {
		}

		public override bool CanConvert(Type fromType, Type toType) {
			if (base.CanConvert(fromType,toType))
				return true;

			if (fromType.GetInterface(typeof(IObjectProvider).FullName)==typeof(IObjectProvider)) {
				// may be conversion from IProvider to toType exists?
				Console.WriteLine("111");
				return TypeManager.CanConvert( typeof(IProvider), toType );
			}

			return false;
		}

		public override object Convert(object o, Type toType) {
			if (base.CanConvert(o.GetType(),toType))
				return base.Convert(o, toType);
			if (o is IObjectProvider) {
				ITypeConverter conv = TypeManager.FindConverter(typeof(IProvider), toType);
				if (conv!=null) {
					object prv = base.Convert(o, typeof(IProvider));
					return conv.Convert(prv, toType);
				}
			}

			throw new InvalidCastException();
		}

	}
}

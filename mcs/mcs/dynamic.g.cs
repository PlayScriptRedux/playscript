
// Generated dynamic class partial classes


namespace Blah1 {

	partial class A : PlayScript.IDynamicClass {

		private PlayScript.IDynamicClass __dynamicProps;

		dynamic PlayScript.IDynamicClass.__GetDynamicValue(string name) {
			object value = null;
			if (__dynamicProps != null) {
				value = __dynamicProps.__GetDynamicValue(name);
			}
			return value;
		}

		bool PlayScript.IDynamicClass.__TryGetDynamicValue(string name, out object value) {
			if (__dynamicProps != null) {
				return __dynamicProps.__TryGetDynamicValue(name, out value);
			} else {
				value = null;
				return false;
			}
		}
			
		void PlayScript.IDynamicClass.__SetDynamicValue(string name, object value) {
			if (__dynamicProps == null) {
				__dynamicProps = new PlayScript.DynamicProperties(this);
			}
			__dynamicProps.__SetDynamicValue(name, value);
		}

		bool PlayScript.IDynamicClass.__DeleteDynamicValue(object name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__DeleteDynamicValue(name);
			}
			return false;
		}
			
		bool PlayScript.IDynamicClass.__HasDynamicValue(string name) {
			if (__dynamicProps != null) {
				return __dynamicProps.__HasDynamicValue(name);
			}
			return false;
		}

		System.Collections.IEnumerable PlayScript.IDynamicClass.__GetDynamicNames() {
			if (__dynamicProps != null) {
				return __dynamicProps.__GetDynamicNames();
			}
			return null;
		}
	}
}


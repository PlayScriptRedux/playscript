
// Generated dynamic class partial classes

using System.Collections.Generic;

namespace _root {

	partial class Foo : PlayScript.IDynamicClass {

		private Dictionary<string, object> __dynamicDict;

		dynamic PlayScript.IDynamicClass.__GetDynamicValue(string name) {
			object value = null;
			if (__dynamicDict != null) {
				__dynamicDict.TryGetValue(name, out value);
			}
			return value;
		}
			
		void PlayScript.IDynamicClass.__SetDynamicValue(string name, object value) {
			if (__dynamicDict == null) {
				__dynamicDict = new Dictionary<string, object>();
			}
			__dynamicDict[name] = value;
		}
			
		bool PlayScript.IDynamicClass.__HasDynamicValue(string name) {
			if (__dynamicDict != null) {
				return __dynamicDict.ContainsKey(name);
			}
			return false;
		}

		_root.Array PlayScript.IDynamicClass.__GetDynamicNames() {
			if (__dynamicDict != null) {
				return new _root.Array(__dynamicDict.Keys);
			}
			return new _root.Array();
		}
	}
}


namespace _root {

	partial class Bar2 : PlayScript.IDynamicClass {

		private Dictionary<string, object> __dynamicDict;

		dynamic PlayScript.IDynamicClass.__GetDynamicValue(string name) {
			object value = null;
			if (__dynamicDict != null) {
				__dynamicDict.TryGetValue(name, out value);
			}
			return value;
		}
			
		void PlayScript.IDynamicClass.__SetDynamicValue(string name, object value) {
			if (__dynamicDict == null) {
				__dynamicDict = new Dictionary<string, object>();
			}
			__dynamicDict[name] = value;
		}
			
		bool PlayScript.IDynamicClass.__HasDynamicValue(string name) {
			if (__dynamicDict != null) {
				return __dynamicDict.ContainsKey(name);
			}
			return false;
		}

		_root.Array PlayScript.IDynamicClass.__GetDynamicNames() {
			if (__dynamicDict != null) {
				return new _root.Array(__dynamicDict.Keys);
			}
			return new _root.Array();
		}
	}
}


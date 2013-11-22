using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript
{
	// this interface allows for the getting or setting of member or indexed values of a certain type T
	// this interface prevents any unnecessary boxing of value types as objects
	public interface IDynamicAccessor<T> 
	{
		T		GetMember(string name, ref uint hint);
		T		GetMemberOrDefault(string name, ref uint hint, T defaultValue);
		void	SetMember(string name, ref uint hint, T value);
		T		GetIndex(string key);
		void	SetIndex(string key, T value);
		T		GetIndex(int key);
		void	SetIndex(int key, T value);
		T		GetIndex(object key);
		void	SetIndex(object key, T value);
	}

	// this interface allows for the getting of member or indexed values that may be undefined
	// this interface allows for the querying of member or index existence and the deleting of members
	public interface IDynamicAccessorUntyped
	{
		// these methods get/set objects that may be undefined (PlayScript.Undefined._undefined)
		[return: AsUntyped]
		object	GetMember(string name, ref uint hint);
		[return: AsUntyped]
		object	GetMemberOrDefault(string name, ref uint hint, [AsUntyped] object defaultValue);
		void	SetMember(string name, ref uint hint, [AsUntyped] object value);
		[return: AsUntyped]
		object	GetIndex(string key);
		void	SetIndex(string key, [AsUntyped] object value);
		[return: AsUntyped]
		object	GetIndex(int key);
		void	SetIndex(int key, [AsUntyped] object value);
		[return: AsUntyped]
		object	GetIndex([AsUntyped] object key);
		void	SetIndex([AsUntyped] object key, [AsUntyped] object value);

		// these are for string keys (object)
		bool	HasMember(string name);
		bool	HasMember(string name, ref uint hint);
		bool	DeleteMember(string name);

		// these are for integer keys (array?)
		bool	HasIndex(int key);
		bool 	DeleteIndex(int key);

		// these are for object keys (dictionary?)
		bool	HasIndex(object key);
		bool	DeleteIndex(object key);

		// returns length of object or array
		int		Count {get;}
	}

	// Typed public accessors
	public interface IDynamicAccessorTyped
	{
		object GetMemberObject (string key, ref uint hint, object defaultValue);
		void SetMemberObject (string key, ref uint hint, object value);
		[return: AsUntyped]
		object GetMemberUntyped (string key, ref uint hint, [AsUntyped] object defaultValue);
		void SetMemberUntyped (string key, ref uint hint, [AsUntyped] object value);
		string GetMemberString (string key, ref uint hint, string defaultValue);
		void SetMemberString (string key, ref uint hint, string value);
		int GetMemberInt (string key, ref uint hint, int defaultValue);
		void SetMemberInt (string key, ref uint hint, int value);
		uint GetMemberUInt (string key, ref uint hint, uint defaultValue);
		void SetMemberUInt (string key, ref uint hint, uint value);
		double GetMemberNumber (string key, ref uint hint, double defaultValue);
		void SetMemberNumber (string key, ref uint hint, double value);
		bool GetMemberBool (string key, ref uint hint, bool defaultValue);
		void SetMemberBool (string key, ref uint hint, bool value);
	}

	// this interface combines all the most common interfaces together into one dynamic object
	// Expando, Dictionary, Array, Vector, BinJson, AmfObject all could implement this
	public interface IDynamicObject
		: 
			IDynamicAccessor<bool>, 
			IDynamicAccessor<int>, 
			IDynamicAccessor<uint>, 
			IDynamicAccessor<float>, 
			IDynamicAccessor<double>, 
			IDynamicAccessor<string>, 
			IDynamicAccessor<object>, 
			IDynamicAccessorUntyped,
			IDynamicAccessorTyped,
			IKeyEnumerable, 
			IEnumerable,
			IEnumerable< KeyValuePair<string, object> >
	{ 
	}

	public interface IDynamicClass
	{

		dynamic __GetDynamicValue(string name);

		bool __TryGetDynamicValue(string name, out object value);

		void __SetDynamicValue(string name, object value);

		bool __DeleteDynamicValue(object name);

		bool __HasDynamicValue(string name);

		IEnumerable __GetDynamicNames();

	}
}


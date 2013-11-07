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
		object	GetMember(string name, ref uint hint);
		void	SetMember(string name, ref uint hint, object value);
		object	GetIndex(string key);
		void	SetIndex(string key, object value);
		object	GetIndex(int key);
		void	SetIndex(int key, object value);
		object	GetIndex(object key);
		void	SetIndex(object key, object value);

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


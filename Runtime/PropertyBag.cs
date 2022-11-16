using System;
using System.Collections;
using System.Reflection;

namespace WebSoft
{
	/// <summary>
	/// The PropertyBag class is designed to be aggregated inside general purpose classes. 
	/// Clients of those classes can then extend their functionality by adding new properties
	/// that were not envisaged by the class designer. If a client attempts to manipulate 
	/// the PropertyBag using a named property that exists in reality in the general purpose
	/// class, then the actual property is reflected over rather than an new property in the
	/// property bag with a duplicated property name.
	/// </summary>
	/// <remarks>
	/// The Property inner class protects the getting and setting of the Value attribute with 
	/// the lock keyword, thus making the PropertyBag object thread safe. 
	/// </remarks>
	/// <author>
	/// Graham Harrison, WebSoft Consultants Limited
	/// </author>
	/// <version>
	/// 1.0
	/// </version>
	//[Serializable()]
	public class PropertyBag : System.Object, IEnumerable
	{
		/// A Hashtable to contain the properties in the bag
		private System.Collections.Hashtable objPropertyCollection = new Hashtable();

		public Hashtable serialtable
		{
			get
			{
				return objPropertyCollection;
			}
			set
			{
				objPropertyCollection = value;
			}
		}
		/// The general purpose class that the PropertyBag instance belongs to
		private System.Object objOwner;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Owner">The owner i.e. parent of the PropertyBag</param>
		public PropertyBag(System.Object Owner)
		{
			this.objOwner = Owner;
		}

		/// <summary>
		/// A pointer to the ultimate client class of the Property / PrpoertyBag
		/// </summary>
		public System.Object Owner
		{
			get
			{
				return this.objOwner;
			}
		}

		/// <summary>
		/// Indexer which retrieves a property from the PropertyBag based on 
		/// the property Name
		/// </summary>
		public Property this[string Name]
		{
			get
			{
				// An instance of the Property that will be returned
				Property objProperty;

				// If the PropertyBag already contains a property whose name matches
				// the property required, ...
				if (objPropertyCollection.Contains(Name))
				{
					// ... then return the pre-existing property
					objProperty = (Property)objPropertyCollection[Name];
				}
				else
				{
					// ... otherwise, create a new Property with a matching Name, and
					// a null Value, and add it to the PropertyBag
					objProperty = new Property(Name, Owner);
					objPropertyCollection.Add(Name, objProperty);
				}
				return objProperty;
			}
		}

		public int getCount()
		{
			return objPropertyCollection.Count;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}

		public PropertyEnum GetEnumerator()
		{
			Property[] _properties = new Property[objPropertyCollection.Count];
			objPropertyCollection.Values.CopyTo(_properties, 0);
			return new PropertyEnum(_properties);
		}

		public class PropertyEnum : IEnumerator
		{
			public Property[] _properties;
			int position = -1;
			public PropertyEnum(Property[] list)
			{
				_properties = list;
			}

			public bool MoveNext()
			{
				position++;
				return (position < _properties.Length);
			}

			public void Reset()
			{
				position = -1;
			}

			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}

			public Property Current
			{
				get
				{
					try
					{
						return _properties[position];
					}
					catch (IndexOutOfRangeException)
					{
						throw new InvalidOperationException();
					}
				}
			}
		}

		/// <summary>
		/// The Property class in an ideal candidate for an inner class to PropertyBag. The ultimate 
		/// clients want to access the class using calls like -
		/// objAddress.Properties["Line1"].Value = "22 North Street";
		/// they are not bothered with individual instantiations of the Property class
		/// </summary>
		//[Serializable()]
		public class Property : System.Object
		{
			private string strName;                         /// Field to hold the name of the property 
			private System.Object objValue;                 /// Field to hold the value of the property
			private System.Object objOwner;                 /// Field to hold the ultimate owner of the property
			public string KeyName                           /// Field to hold the public name of the property 
			{
				get
				{
					return strName;
				}
			}

			private uint m_dirtybit;

			public event UpdatingEventHandler Updating; /// Event fires immediately prior to value changes
			public event UpdatedEventHandler Updated;   /// Event fires immediately prior to value changes

														/// <summary>
														/// Constructor
														/// </summary>
														/// <param name="Name">The name of the new property</param>
														/// <param name="Owner">The owner i.e. parent of the PropertyBag</param>
			public Property(string Name, Object Owner)
			{
				this.strName = Name;
				this.objOwner = Owner;
				this.objValue = null;
			}

			/// <summary>
			/// The property value
			/// </summary>
			public System.Object Value
			{
				get
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to get the value of the Property at the same time
					lock (this)
					{
						// Use reflection to see if the client class has a "real" property 
						// that is named the same as the property that we are attempting to
						// retrieve from the PropertyBag
						System.Reflection.PropertyInfo p = Owner.GetType().GetProperty(Name);

						if (p != null)
						{
							// If the client class does have a real property thus named, return 
							// the current value of that "real" property
							return p.GetValue(Owner, new object[] { });
						}
						else
						{
							// If the client class does not have a real property thus named, 
							// return this Property objects Value attribute
							System.Reflection.FieldInfo f = Owner.GetType().GetField(Name);
							if (f != null)
								return f.GetValue(Owner);
							else
								return this.objValue;
						}
					}
				}
				set
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to set the value of the Property at the same time
					lock (this)
					{
						// Reflection is used to see if the client class has a "real" property 
						// that is named the same as the property that we are attempting to
						// set in the PropertyBag
						System.Reflection.PropertyInfo objPropertyInfo = Owner.GetType().GetProperty(Name);
						System.Reflection.FieldInfo f = Owner.GetType().GetField(Name);

						// Placeholder for the old value
						System.Object objOldValue = null;


						// If pbjPropertyInfo is not null, ...
						if (objPropertyInfo != null)
						{
							// ... then the client class has a real property thus named, 
							// save the current value of that real property into objOldValue
							objOldValue = objPropertyInfo.GetValue(Owner, new object[] { });
						}
						else
						{
							// ... otherwise the client class does not have a real property thus 
							// named, save the current value of this Property objects Value attribute
							if (f != null)
								objOldValue = f.GetValue(Owner);
							else
								objOldValue = this.objValue;
						}

						// Create a sub-class of EventArgs to hold the event arguments
						WebSoft.UpdatingEventArgs objUpdatingEventArgs = new UpdatingEventArgs(Name, objOldValue, value);

						// Execute a synchronous call to each subscriber
						OnUpdating(objUpdatingEventArgs);

						// If one or more subscribers set the Cancel property to true, this means that
						// the update is cancelled in a OnBeforeUpdate event (maybe validation has 
						// failed), so the the function returns immediately
						if (objUpdatingEventArgs.Cancel)
						{
							return;
						}

						// If the client class has a "real" property matching this Property Name, ...
						if (objPropertyInfo != null)
						{
							// ... then set that "real" property to the new value
							objPropertyInfo.SetValue(Owner, value, new object[] { });
						}
						else
						{
							// ... otherwise, set the Value attribute of the current property objectobjOldValue
							if (f != null)
								f.SetValue(Owner, value);
							else
								this.objValue = value;
						}

						// ... Execute a synchronous call to each subscriber
						OnUpdated(new UpdatedEventArgs(Name, objOldValue, value));
					}
				}
			}

			/// <summary>
			/// Raises the Updating Event
			/// </summary>
			/// <param name="e">The updating event args</param>
			protected void OnUpdating(UpdatingEventArgs e)
			{
				if (this.Updating != null)
				{
					this.Updating(this, e);
				}
			}

			/// <summary>
			/// Raises the Updated Event
			/// </summary>
			/// <param name="e">The updated event args</param>
			protected void OnUpdated(UpdatedEventArgs e)
			{
				if (this.Updated != null)
				{
					this.Updated(this, e);
				}
			}

			/// <summary>
			/// The name of the Property
			/// </summary>
			public string Name
			{
				get
				{
					return this.strName;
				}
			}

			/// <summary>
			/// A pointer to the ultimate client class of the Property / PrpoertyBag
			/// </summary>
			public System.Object Owner
			{
				get
				{
					return this.objOwner;
				}
			}

			/// <summary>
			/// The PropertyInfo
			/// </summary>
			public System.Reflection.PropertyInfo propertyInfo
			{
				get
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to get the value of the Property at the same time
					lock (this)
					{
						// Use reflection to see if the client class has a "real" property 
						// that is named the same as the property that we are attempting to
						// retrieve from the PropertyBag
						return Owner.GetType().GetProperty(Name, BindingFlags.Instance | BindingFlags.Public);
					}
				}
			}

			/// <summary>
			/// The PropertyInfo
			/// </summary>
			public System.Reflection.FieldInfo fieldInfo
			{
				get
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to get the value of the Property at the same time
					lock (this)
					{
						// Use reflection to see if the client class has a "real" property 
						// that is named the same as the property that we are attempting to
						// retrieve from the PropertyBag
						return Owner.GetType().GetField(Name, BindingFlags.Instance | BindingFlags.Public);
					}
				}
			}

			public void SetDirty()
			{
				m_dirtybit = 1U;
			}
			public void SetDirty(uint dirtybit)
			{
				m_dirtybit |= dirtybit;
			}

			public void ClearDirty()
			{
				m_dirtybit = 0U;
			}

			public void ClearDirty(uint dirtybit)
			{
				m_dirtybit |= dirtybit;
			}

			public bool isDirty()
			{
				return m_dirtybit != 0U;
			}
		}
	}

	/// <summary>
	/// A sub-class of EventArgs that contains the customevent arguments 
	/// for the OnUpdated event
	/// </summary>
	public class UpdatedEventArgs : EventArgs
	{
		// The Property Name
		private string strName;
		// The Property value before the change
		private System.Object objOldValue;
		// The Property value after the change
		private System.Object objNewValue;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Name">The Property Name</param>
		/// <param name="OldValue">The Property value before the change</param>
		/// <param name="NewValue">The Property value after the change</param>
		public UpdatedEventArgs(string Name, System.Object OldValue, System.Object NewValue)
		{
			this.strName = Name;
			this.objOldValue = OldValue;
			this.objNewValue = NewValue;
		}

		/// <summary>
		/// The Property Name
		/// </summary>
		public string Name
		{
			get
			{
				return this.strName;
			}
		}

		/// <summary>
		/// The Property value before the change
		/// </summary>
		public System.Object OldValue
		{
			get
			{
				return this.objOldValue;
			}
		}

		/// <summary>
		/// The Property value after the change
		/// </summary>
		public System.Object NewValue
		{
			get
			{
				return this.objNewValue;
			}
		}
	}

	/// <summary>
	/// A sub-class of UpdatedEventArgs (and hence EventArgs) that contains 
	/// the customevent arguments for the OnUpdating event
	/// </summary>
	public class UpdatingEventArgs : UpdatedEventArgs
	{
		// Indicator that a proposed update should be cancelled
		private bool blnCancel;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Name">The Property Name</param>
		/// <param name="OldValue">The Property value before the change</param>
		/// <param name="NewValue">The Property value after the change</param>
		public UpdatingEventArgs(string Name, System.Object OldValue, System.Object NewValue) : base(Name, OldValue, NewValue)
		{
			// Cancel defaults to false
			this.blnCancel = false;
		}

		/// <summary>
		/// Indicates whether a propised update from OldValue to NewValue
		/// should be cancelled i.e. the Value will remain at OldValue
		/// </summary>
		public bool Cancel
		{
			get
			{
				// The lock statement makes the class thread safe. Multiple subscriber threads 
				// can attempt to get the value of Cancel at the same time
				lock (this)
				{
					return this.blnCancel;
				}
			}
			set
			{
				// The lock statement makes the class thread safe. Multiple subscriber threads 
				// can attempt to set the value of Cancel at the same time
				lock (this)
				{
					if (value == true)
					{
						this.blnCancel = value;
					}
				}
			}
		}
	}

	/// <summary>
	/// Interface definition for the OnUpdating event
	/// </summary>
	public delegate void UpdatingEventHandler(object sender, UpdatingEventArgs e);

	/// <summary>
	/// Interface definition for the OnUpdated event
	/// </summary>
	public delegate void UpdatedEventHandler(object sender, UpdatedEventArgs e);

}

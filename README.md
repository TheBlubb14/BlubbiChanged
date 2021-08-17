# BlubbiChanged
Automatically generate code to add INotifyPropertyChanged and INotifyPropertyChanging to your properties.

#### What you write
``` cs
partial class Program
{
    /// <summary>
    /// That is my name
    /// </summary>
    [AutoNotify]
    private string myName;
}
```

#### What will be generated
``` cs
partial class Program : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	/// <inheritdoc/>
	public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

	/// <inheritdoc/>
	public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// That is my name
	/// </summary>
	public string MyName
	{
        get => this.myName;
		set
		{
			if (global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(this.myName, value))
				return;

			this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs("MyName"));

			this.myName = value;

			this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs("MyName"));
		}
	}
}
```

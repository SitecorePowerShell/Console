using System;
using System.Management.Automation;
using System.Reflection;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Spe.Core.Extensions;

public class PsItemProperty: PSPropertyInfo
{
  //private readonly string name;
  private readonly Item item;
  private readonly Field field;

  /// <summary>Returns the string representation of this property</summary>
  /// <returns>This property as a string</returns>
  public override string ToString()
  {
    return $"{item.Uri}=>{Name}"; 
  }

  /// <summary>
  /// Used from TypeTable to delay setting getter and setter
  /// </summary>
  internal PsItemProperty(Item propertyItem, string propertyName, Field field)
  {
    var type = typeof(PSMemberInfo);
    // Get the internal field
    var fieldInfo = type.GetField("name",
      BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    if (fieldInfo == null)
      throw new InvalidOperationException("Field not found");

    // Set the value on this instance
    fieldInfo.SetValue(this, propertyName);    
    item = propertyItem;
    this.field = field;
  }

  public override PSMemberInfo Copy()
  {
    return new PsItemProperty(item, Name, field);
  }

  public override PSMemberTypes MemberType => PSMemberTypes.CodeProperty;
  public override bool IsSettable => true;
  public override bool IsGettable => true;
  public override string TypeNameOfValue => typeof(string).FullName;

  public override object Value
  {
    get
    {
      if (field.TypeKey == "datetime")
      {
        return Sitecore.DateUtil.IsoDateToDateTime(field.Value);
      }
      else
      {
        return field?.Value ?? "";
      }
      
    }
    set => ItemShellExtensions.ModifyProperty(item, field.ID, value);
  }
}

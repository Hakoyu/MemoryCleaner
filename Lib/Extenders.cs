using System;
using System.Reflection;
using System.ComponentModel;

namespace Extenders
{
    static class Extender
    {
        /// <summary>
        /// <para>获取枚举描述</para>
        /// <para>在枚举项前一行添加[System.ComponentModel.Description("描述")]即可</para>
        /// <para>!!!警告!!! 请确保使用的是函数GetDescription()而并非委托GetDescription</para>
        /// </summary>
        /// <param name="en">枚举类型</param>
        /// <returns>枚举设置的描述</returns>
        public static string GetDescription(this Enum en)
        {
            //获取字段
            if (en.GetType().GetField(en.ToString()) is FieldInfo field)
                //判断是否有描述
                if ((Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute))
                    //返回描述
                    return attribute.Description;
            return en.ToString();
        }
    }
}
﻿using System;

namespace ZKWebStandard.Ioc {
#pragma warning disable S3376 // Attribute, EventArgs, and Exception type names should end with the type being extended
	/// <summary>
	/// Base attribute for register type to IoC container<br/>
	/// 注册类型到IoC容器使用的属性的基类<br/>
	/// </summary>
	public abstract class ExportAttributeBase : Attribute {
#pragma warning restore S3376 // Attribute, EventArgs, and Exception type names should end with the type being extended
		/// <summary>
		/// Register implementation type to container<br/>
		/// 注册实现类型到容器<br/>
		/// </summary>
		/// <param name="container">Container</param>
		/// <param name="type">Implementation type</param>
		/// <param name="reuseType">Reuse type</param>
		public abstract void RegisterToContainer(IContainer container, Type type, ReuseType reuseType);
	}
}

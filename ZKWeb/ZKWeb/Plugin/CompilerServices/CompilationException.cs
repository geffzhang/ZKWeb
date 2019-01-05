﻿using System;

namespace ZKWeb.Plugin.CompilerServices {
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
	/// <summary>
	/// Exception for compilation error<br/>
	/// 表示编译错误的例外<br/>
	/// </summary>
	public class CompilationException : Exception {
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
		/// <summary>
		/// Initialize<br/>
		/// 初始化<br/>
		/// </summary>
		/// <param name="message">Error message</param>
		public CompilationException(string message) : base(message) { }
	}
}

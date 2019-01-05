﻿using System;
using ZKWebStandard.Extensions;
using ZKWebStandard.Testing;

namespace ZKWebStandard.Tests.Extensions {
	[Tests]
	class ExceptionExtensionsTest {
		public void ToDetailedString() {
			var ex = new ArgumentException("qwert", new ArgumentException("inner qwert"));
			var str = ex.ToDetailedString();
			Assert.IsTrueWith(str.Contains("inner qwert"), str);
		}

		public void TestSummaryString() {
			var ex = new ArgumentException("qwert", new ArgumentException("inner qwert"));
			var str = ex.ToSummaryString();
			Assert.IsTrueWith(str.Contains("inner qwert"), str);
		}
	}
}

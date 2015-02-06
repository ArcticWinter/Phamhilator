﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Phamhilator.Core;



namespace Phamhilator.Tests
{
    [TestClass]
    public class ExtensionMethodsTest
    {
        [TestMethod]
        public void IsFlakRegexTest()
        {
            var flak0 = new Regex(@"^([a-zA-Z0-9])(([\-.]|[_]+)?([a-zA-Z0-9]+))*(@){1}[a-z0-9]+[.]{1}(([a-z]{2,3})|(‌​‌​[a-z]{2,3}[.]{1}[a-z]{2,3}))$");
            var flak1 = new Regex(@"(.|.+|.*){20}[A-Z]");
            var flak2 = new Regex(@"\w+.(\d.{2}\w)+");
            var real0 = new Regex(@"$.*^");
            var real1 = new Regex(@"\d*");
            var real2 = new Regex(@"[^a-zA-Z0-9]*");

            var isFlak = flak0.IsFlakRegex();
            Assert.IsTrue(isFlak);

            isFlak = flak1.IsFlakRegex();
            Assert.IsTrue(isFlak);

            isFlak = flak2.IsFlakRegex();
            Assert.IsTrue(isFlak);

            isFlak = real0.IsFlakRegex();
            Assert.IsFalse(isFlak);

            isFlak = real1.IsFlakRegex();
            Assert.IsFalse(isFlak);

            isFlak = real2.IsFlakRegex();
            Assert.IsFalse(isFlak);
        }

        [TestMethod]
        public void IsQuestionTest()
        {
            var questionClasses = new[]
            { 
                FilterClass.QuestionBodyLQ, FilterClass.QuestionBodyOff,
                FilterClass.QuestionBodySpam, FilterClass.QuestionTitleLQ,
                FilterClass.QuestionTitleName, FilterClass.QuestionTitleOff,
                FilterClass.QuestionTitleSpam
            };

            foreach (var classification in Enum.GetValues(typeof(FilterClass)).Cast<FilterClass>())
            {
                var isQuestion = classification.IsQuestion();

                if (questionClasses.Contains(classification))
                {
                    Assert.IsTrue(isQuestion);
                }
                else
                {
                    Assert.IsFalse(isQuestion);
                }
            }
        }

        [TestMethod]
        public void IsQuestionTitleTest()
        {
            var questionTitleClasses = new[]
            { 
                FilterClass.QuestionTitleLQ, FilterClass.QuestionTitleOff,
                FilterClass.QuestionTitleName, FilterClass.QuestionTitleSpam
            };

            foreach (var classification in Enum.GetValues(typeof(FilterClass)).Cast<FilterClass>())
            {
                var isQuestion = classification.IsQuestionTitle();

                if (questionTitleClasses.Contains(classification))
                {
                    Assert.IsTrue(isQuestion);
                }
                else
                {
                    Assert.IsFalse(isQuestion);
                }
            }
        }
    }
}
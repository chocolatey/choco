// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.commandline;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace chocolatey.tests.infrastructure.commandline
{
    public class InteractivePromptSpecs
    {
        public abstract class InteractivePromptSpecsBase : TinySpec
        {
            protected Mock<IConsole> Console = new Mock<IConsole>();
            protected IList<string> Choices = new List<string>();
            protected string PromptValue;

            public override void Context()
            {
                PromptValue = "hi";

                InteractivePrompt.InitializeWith(new Lazy<IConsole>(() => Console.Object));

                Choices.Add("yes");
                Choices.Add("no");
            }

            public void Should_have_called_Console_ReadLine()
            {
                Console.Verify(c => c.ReadLine(), Times.AtLeastOnce);
            }
        }

        public class When_prompting_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private string _defaultChoice;
            private Func<string> _prompt;

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, _defaultChoice, requireAnswer: false);
            }

            [Fact]
            public void Should_error_when_the_choicelist_is_null()
            {
                Choices = null;
                var errored = false;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_choicelist_is_empty()
            {
                Choices = new List<string>();
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Contain("No choices passed in.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_prompt_input_is_null()
            {
                Choices = new List<string>
                {
                    "bob"
                };
                PromptValue = null;
                var errored = false;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_default_choice_is_not_in_list()
            {
                Choices = new List<string>
                {
                    "bob"
                };
                _defaultChoice = "maybe";
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                string result = null;
                try
                {
                    result = _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                result.Should().NotBe("maybe");
                errored.Should().BeTrue();
                errorMessage.Should().Be("Default choice value must be one of the given choices.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }
        }

        public class When_prompting_with_interactivePrompt_without_default_and_answer_is_not_required : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: false);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_return_null_when_no_answer_given()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_first_choice_when_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_first_choice_when_value_of_choice_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("yes");
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_second_choice_when_2_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("2");
                var result = _prompt();
                result.Should().Be(Choices[1]);
            }

            [Fact]
            public void Should_return_null_choice_when_3_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("3");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_4_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("4");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_0_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("0");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_negative_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("-1");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_alphabetical_characters_are_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("abc");
                var result = _prompt();
                result.Should().BeNull();
            }
        }

        public class When_prompting_with_interactivePrompt_without_default_and_answer_is_required : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_error_when_no_answer_given()
            {
                var errored = false;

                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }

            [Fact]
            public void Should_return_first_choice_when_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_second_choice_when_2_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("2");
                var result = _prompt();
                result.Should().Be(Choices[1]);
            }

            [Fact]
            public void Should_error_when_any_choice_not_available_is_given()
            {
                var errored = false;

                Console.Setup(c => c.ReadLine()).Returns("3"); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }

        public class When_prompting_with_interactivePrompt_with_default_and_answer_is_not_required : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, Choices[1], requireAnswer: false);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_return_default_when_no_answer_given()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = _prompt();
                result.Should().Be(Choices[1]);
            }

            [Fact]
            public void Should_return_first_choice_when_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_second_choice_when_2_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("2");
                var result = _prompt();
                result.Should().Be(Choices[1]);
            }

            [Fact]
            public void Should_return_null_choice_when_3_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("3");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_4_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("4");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_0_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("0");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_negative_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("-1");
                var result = _prompt();
                result.Should().BeNull();
            }

            [Fact]
            public void Should_return_null_choice_when_alphabetical_characters_are_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("abc");
                var result = _prompt();
                result.Should().BeNull();
            }
        }

        public class When_prompting_with_interactivePrompt_with_default_and_answer_is_required : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, Choices[0], requireAnswer: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_error_when_no_answer_given()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_first_choice_when_1_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                var result = _prompt();
                result.Should().Be(Choices[0]);
            }

            [Fact]
            public void Should_return_second_choice_when_2_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("2");
                var result = _prompt();
                result.Should().Be(Choices[1]);
            }

            [Fact]
            public void Should_error_when_any_choice_not_available_is_given()
            {
                var errored = false;

                Console.Setup(c => c.ReadLine()).Returns("3"); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }

        public class When_prompting_short_with_interactivePrompt_guard_errors : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, defaultChoice: null, requireAnswer: true, shortPrompt: true);
            }

            [Fact]
            public void Should_error_when_the_choicelist_is_null()
            {
                Choices = null;
                var errored = false;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_choicelist_is_empty()
            {
                Choices = new List<string>();
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Contain("No choices passed in.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_prompt_input_is_null()
            {
                Choices = new List<string>
                {
                    "bob"
                };
                PromptValue = null;
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Contain("Value for prompt cannot be null.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_choicelist_contains_empty_values()
            {
                Choices = new List<string>
                {
                    "bob",
                    ""
                };
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Contain("Some choices are empty.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void Should_error_when_the_choicelist_has_multiple_items_with_same_first_letter()
            {
                Choices = new List<string>
                {
                    "sally",
                    "suzy"
                };
                var errored = false;
                var errorMessage = string.Empty;
                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Contain("Multiple choices have the same first letter.");
                Console.Verify(c => c.ReadLine(), Times.Never);
            }
        }

        public class When_prompting_short_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, defaultChoice: null, requireAnswer: true, shortPrompt: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_error_when_no_answer_given()
            {
                var errored = false;

                Console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }

            [Fact]
            public void Should_return_yes_when_yes_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("yes");
                var result = _prompt();
                result.Should().Be("yes");
            }

            [Fact]
            public void Should_return_yes_when_y_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("y");
                var result = _prompt();
                result.Should().Be("yes");
            }

            [Fact]
            public void Should_return_no_choice_when_no_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("no");
                var result = _prompt();
                result.Should().Be("no");
            }

            [Fact]
            public void Should_return_no_choice_when_n_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("n");
                var result = _prompt();
                result.Should().Be("no");
            }

            [Fact]
            public void Should_error_when_any_choice_not_available_is_given()
            {
                var errored = false;

                Console.Setup(c => c.ReadLine()).Returns("yup"); //Enter pressed
                try
                {
                    _prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.Should().BeTrue();
                Console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }

        public class When_prompting_answer_with_dash_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;

            public override void Context()
            {
                base.Context();
                Choices.Add("all scripts");
            }

            public override void Because()
            {
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, defaultChoice: null, requireAnswer: true, shortPrompt: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                Should_have_called_Console_ReadLine();
            }

            [Fact]
            public void Should_return_all_when_full_all_answer_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("all scripts");
                var result = _prompt();
                result.Should().Be("all scripts");
            }
        }

        public class When_prompting_with_timeout_and_default_choice : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;
            private string _result;

            public override void Because()
            {
                Console.Setup(c => c.ReadLine(It.IsAny<int>())).Returns((string)null);
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, Choices[0], requireAnswer: false, timeoutInSeconds: 1);
                _result = _prompt();
            }

            [Fact]
            public void Should_return_default_choice_when_timed_out()
            {
                _result.Should().Be(Choices[0]);
            }
        }

        public class When_prompting_with_timeout_and_no_default_choice : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;
            private string _result;

            public override void Because()
            {
                Console.Setup(c => c.ReadLine(It.IsAny<int>())).Returns((string)null);
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: false, timeoutInSeconds: 1);
                _result = _prompt();
            }

            [Fact]
            public void Should_return_null_when_timed_out_and_no_default()
            {
                _result.Should().BeNull();
            }
        }
        public class When_prompting_with_dash_in_choices : InteractivePromptSpecsBase
        {
            private Func<string> _prompt;
            private string _result;

            public override void Context()
            {
                base.Context();
                Choices.Add("all scripts");
            }

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns("all scripts");
                _prompt = () => InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
                _result = _prompt();
            }

            [Fact]
            public void Should_match_choice_that_contains_space()
            {
                _result.Should().Be("all scripts");
            }
        }
        public class When_getting_password_and_only_enter_pressed : TinySpec
        {
            private string _result;
            private Mock<IConsole> _console;

            public override void Context()
            {
                _console = new Mock<IConsole>();
                _console.Setup(c => c.ReadKey(It.IsAny<bool>()))
                    .Returns(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
                InteractivePrompt.InitializeWith(new Lazy<IConsole>(() => _console.Object));
            }

            public override void Because()
            {
                _result = InteractivePrompt.GetPassword(interactive: true);
            }

            [Fact]
            public void Should_return_empty_string()
            {
                _result.Should().BeEmpty();
            }
        }
        public class When_prompting_and_invalid_input_is_followed_by_valid_input : InteractivePromptSpecsBase
        {
            private string _result;

            public override void Because()
            {
                Console.SetupSequence(c => c.ReadLine())
                    .Returns("invalid")
                    .Returns("")
                    .Returns("2");

                _result = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
            }

            [Fact]
            public void Should_return_second_choice_after_retry()
            {
                _result.Should().Be("no");
            }

            [Fact]
            public void Should_have_retried_on_invalid_input()
            {
                Console.Verify(c => c.ReadLine(), Times.Exactly(3));
            }
        }

        public class When_prompting_with_case_insensitive_default_choice : InteractivePromptSpecsBase
        {
            private string _result;

            public override void Context()
            {
                Choices = new List<string> { "Yes", "No" };
                PromptValue = "continue?";
                base.Context();
            }

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); // Enter
                _result = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, defaultChoice: "yes", requireAnswer: false);
            }

            [Fact]
            public void Should_allow_case_mismatched_default_choice()
            {
                _result.Should().Be("yes"); // the exact value passed as defaultChoice
            }

            [Fact]
            public void Should_be_considered_valid_if_case_mismatch_exists_but_string_present()
            {
                Choices.Should().Contain(c => c.Equals("yes", StringComparison.OrdinalIgnoreCase));
            }
        }

        public class When_prompting_with_multiple_choices_having_same_first_letter : InteractivePromptSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                Choices = new List<string> { "yes", "yell" };
                base.Context();
            }

            public override void Because()
            {
                try
                {
                    InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: false);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_throw_validation_exception_for_non_unique_shortcut_letters()
            {
                _thrown.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_explain_first_letter_conflict()
            {
                _thrown.Message.Should().Contain("Multiple choices have the same first letter");
            }
        }

        public class When_prompting_with_invalid_default_choice : InteractivePromptSpecsBase
        {
            private Exception _thrown;

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns(""); // enter
                try
                {
                    InteractivePrompt.PromptForConfirmation(PromptValue, Choices, "maybe", requireAnswer: false);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_throw_application_exception()
            {
                _thrown.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_report_invalid_default_choice()
            {
                _thrown.Message.Should().Be("Default choice value must be one of the given choices.");
            }
        }

        public class When_prompting_with_default_choice_with_extra_whitespace : InteractivePromptSpecs.InteractivePromptSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                Choices = new List<string> { "yes", "no" };
                base.Context();
            }

            public override void Because()
            {
                try
                {
                    InteractivePrompt.PromptForConfirmation(PromptValue, Choices, " yes ", requireAnswer: false);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_throw_for_invalid_default_choice_with_whitespace()
            {
                _thrown.Should().BeOfType<ApplicationException>();
                _thrown.Message.Should().Be("Default choice value must be one of the given choices.");
            }
        }

        public class When_prompting_with_choices_containing_dashes_and_input_is_prefix : InteractivePromptSpecs.InteractivePromptSpecsBase
        {
            private string _result;

            public override void Context()
            {
                Choices = new List<string> { "1 - all scripts", "2 - cancel" };
                base.Context();
            }

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                _result = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
            }

            [Fact]
            public void Should_match_choice_using_prefix_before_dash()
            {
                _result.Should().Be("1 - all scripts");
            }
        }

        public class When_prompting_with_empty_prompt_string : InteractivePromptSpecs.InteractivePromptSpecsBase
        {
            private string _result;

            public override void Context()
            {
                PromptValue = "";
                base.Context();
            }

            public override void Because()
            {
                Console.Setup(c => c.ReadLine()).Returns("1");
                _result = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: false);
            }

            [Fact]
            public void Should_allow_empty_prompt_and_return_choice()
            {
                _result.Should().Be("yes");
            }
        }

        public class When_prompting_multiple_times_in_sequence : InteractivePromptSpecs.InteractivePromptSpecsBase
        {
            private string _first;
            private string _second;

            public override void Because()
            {
                Console.SetupSequence(c => c.ReadLine())
                    .Returns("1")
                    .Returns("2");

                _first = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
                _second = InteractivePrompt.PromptForConfirmation(PromptValue, Choices, null, requireAnswer: true);
            }

            [Fact]
            public void Should_return_first_choice_for_first_call()
            {
                _first.Should().Be("yes");
            }

            [Fact]
            public void Should_return_second_choice_for_second_call()
            {
                _second.Should().Be("no");
            }
        }

        public class When_getting_password_with_typing : TinySpec
        {
            private string _result;
            private Mock<IConsole> _console;

            public override void Context()
            {
                _console = new Mock<IConsole>();
                _console.SetupSequence(c => c.ReadKey(It.IsAny<bool>()))
                    .Returns(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false))
                    .Returns(new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false))
                    .Returns(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

                InteractivePrompt.InitializeWith(new Lazy<IConsole>(() => _console.Object));
            }

            public override void Because()
            {
                _result = InteractivePrompt.GetPassword(interactive: true);
            }

            [Fact]
            public void Should_capture_typed_password_characters()
            {
                _result.Should().Be("ab");
            }
        }

    }
}

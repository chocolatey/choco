// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.commandline
{
    using System;
    using System.Collections.Generic;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using FluentAssertions;

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
                bool errored = false;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;

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
                bool errored = false;

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
                bool errored = false;

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
                bool errored = false;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;
                string errorMessage = string.Empty;
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
                bool errored = false;

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
                bool errored = false;

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
                Choices.Add("all - yes to all");
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
                Console.Setup(c => c.ReadLine()).Returns("all - yes to all");
                var result = _prompt();
                result.Should().Be("all - yes to all");
            }

            [Fact]
            public void Should_return_all_when_only_all_is_given()
            {
                Console.Setup(c => c.ReadLine()).Returns("all");
                var result = _prompt();
                result.Should().Be("all - yes to all");
            }
        }
    }
}

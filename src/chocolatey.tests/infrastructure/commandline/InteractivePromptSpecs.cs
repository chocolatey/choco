// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using Should;

    public class InteractivePromptSpecs
    {
        public abstract class InteractivePromptSpecsBase : TinySpec
        {
            protected Mock<IConsole> console = new Mock<IConsole>();
            protected IList<string> choices = new List<string>();
            protected string prompt_value;

            public override void Context()
            {
                prompt_value = "hi";

                InteractivePrompt.initialize_with(new Lazy<IConsole>(() => console.Object));

                choices.Add("yes");
                choices.Add("no");
            }

            public void should_have_called_Console_ReadLine()
            {
                console.Verify(c => c.ReadLine(), Times.AtLeastOnce);
            }
        }

        public class when_prompting_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private string default_choice;
            private Func<string> prompt;

            public override void Because()
            {
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, default_choice, requireAnswer: false);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_null()
            {
                choices = null;
                bool errored = false;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_empty()
            {
                choices = new List<string>();
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("No choices passed in.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_prompt_input_is_null()
            {
                choices = new List<string>
                {
                    "bob"
                };
                prompt_value = null;
                bool errored = false;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_default_choice_is_not_in_list()
            {
                choices = new List<string>
                {
                    "bob"
                };
                default_choice = "maybe";
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                string result = null;
                try
                {
                    result = prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                result.ShouldNotEqual("maybe");
                errored.ShouldBeTrue();
                errorMessage.ShouldEqual("Default choice value must be one of the given choices.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }
        }

        public class when_prompting_with_interactivePrompt_without_default_and_answer_is_not_required : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, null, requireAnswer: false);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_return_null_when_no_answer_given()
            {
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_first_choice_when_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("1");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_first_choice_when_value_of_choice_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("yes");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_second_choice_when_2_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("2");
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_return_null_choice_when_3_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("3");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_4_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("4");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_0_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("0");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_negative_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("-1");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_alphabetical_characters_are_given()
            {
                console.Setup(c => c.ReadLine()).Returns("abc");
                var result = prompt();
                result.ShouldBeNull();
            }
        }

        public class when_prompting_with_interactivePrompt_without_default_and_answer_is_required : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, null, requireAnswer: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_error_when_no_answer_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }

            [Fact]
            public void should_return_first_choice_when_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("1");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_second_choice_when_2_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("2");
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_error_when_any_choice_not_available_is_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns("3"); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }

        public class when_prompting_with_interactivePrompt_with_default_and_answer_is_not_required : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, choices[1], requireAnswer: false);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_return_default_when_no_answer_given()
            {
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_return_first_choice_when_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("1");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_second_choice_when_2_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("2");
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_return_null_choice_when_3_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("3");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_4_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("4");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_0_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("0");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_negative_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("-1");
                var result = prompt();
                result.ShouldBeNull();
            }

            [Fact]
            public void should_return_null_choice_when_alphabetical_characters_are_given()
            {
                console.Setup(c => c.ReadLine()).Returns("abc");
                var result = prompt();
                result.ShouldBeNull();
            }
        }

        public class when_prompting_with_interactivePrompt_with_default_and_answer_is_required : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, choices[0], requireAnswer: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_error_when_no_answer_given()
            {
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_first_choice_when_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("1");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_second_choice_when_2_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("2");
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_error_when_any_choice_not_available_is_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns("3"); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }

        public class when_prompting_short_with_interactivePrompt_guard_errors : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, defaultChoice: null, requireAnswer: true, shortPrompt: true);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_null()
            {
                choices = null;
                bool errored = false;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_empty()
            {
                choices = new List<string>();
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("No choices passed in.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_prompt_input_is_null()
            {
                choices = new List<string>
                {
                    "bob"
                };
                prompt_value = null;
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("Value for prompt cannot be null.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_choicelist_contains_empty_values()
            {
                choices = new List<string>
                {
                    "bob",
                    ""
                };
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("Some choices are empty.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_choicelist_has_multiple_items_with_same_first_letter()
            {
                choices = new List<string>
                {
                    "sally",
                    "suzy"
                };
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("Multiple choices have the same first letter.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }
        }

        public class when_prompting_short_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices, defaultChoice: null, requireAnswer: true, shortPrompt: true);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_error_when_no_answer_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }

            [Fact]
            public void should_return_yes_when_yes_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("yes");
                var result = prompt();
                result.ShouldEqual("yes");
            }

            [Fact]
            public void should_return_yes_when_y_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("y");
                var result = prompt();
                result.ShouldEqual("yes");
            }

            [Fact]
            public void should_return_no_choice_when_no_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("no");
                var result = prompt();
                result.ShouldEqual("no");
            }

            [Fact]
            public void should_return_no_choice_when_n_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("n");
                var result = prompt();
                result.ShouldEqual("no");
            }

            [Fact]
            public void should_error_when_any_choice_not_available_is_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns("yup"); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }
    }
}

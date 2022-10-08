# clmath
### A commandline tool for evaluating algebraic functions and displaying those

## Features
#### Simple math evaluation
clmath can evaluate simple math expressions either as program arguments, or inside the math shell.

![Simple math usage](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/simple_math.png)

#### Algebraic function evaluation
clmath supports a strong toolset to store, display and evaluate algebraic functions.
A function can be loaded as program arguments, or inside the math shell.

Functions support an infinite amount of variables, which need to be defined before evaluation.

![Simple functions evaluation with 1 variable](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/functions_1.png)

![Simple functions evaluation with 2 variables](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/functions_2.png)

Once loaded, a function can be stored with a simple name to be loaded later or to be re-used inside other functions.

![Saving and loading functions](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/function_saving.png)

##### Function variables
Function variables may be defined as constant values, a subordinate term or an entire evaluation of another saved function.

![Defining variables as subterms](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/variables_1.png)

##### Function Graph
Functions that have exactly 1 variable can be displayed in a Graph using the `graph` command.
When in the normal math shell, or launching through program arguments, the graph can display up to 6 different functions by using `graph <function_1> <function_2> <function_n>`.

![Using the Graph](https://raw.githubusercontent.com/comroid-git/clmath/master/docs/graph_1.png)

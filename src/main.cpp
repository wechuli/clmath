#include <iostream>
#include "FuncVisitor.h"
#include "MathLexer.h"
#include "Component.h"

using namespace std;

double* readVars(string* keys) {
    int len = sizeof(keys);
    auto vars = (double*) malloc(len);
    string line;
    for (int i = 0; i < len; i++) {
        cout << "input> " + keys[i] + " = ";
        getline(cin, line);
        vars[i] = atof(line.c_str());
    }
    return vars;
}

int eval(string* func) {
    // parse function
    auto stream = new antlr4::ANTLRInputStream(func->c_str());
    auto lexer = new MathLexer(stream);
    auto tokens = new antlr4::CommonTokenStream(lexer);
    auto parser = new MathParser(tokens);
    auto visited = FuncVisitor().visit(parser->expr());
    auto comp = any_cast<Component>(visited);

    MathContext ctx = MathContext();

    // extract variables
    vector<string> keyVec = comp.getKeys();
    int len = keyVec.size();
    if (len != 0) {
        string *keys = &keyVec[0];
        double *vars = readVars(keys);

        // push variables into context
        for (int i = 0; i < len; i++)
            ctx.var->insert(pair(keys[i], vars[i]));
    }

    cout << any_cast<double>(comp.evaluate(&ctx));
    return 0;
}

int cliMode() {
    while (true) {
        cout << "math> ";
        string func;
        getline(cin, func);

        if (func == "exit")
            return 0;
        eval(&func);
    }
}

int main(int argc, char** argv) {
    if (argc >= 1) {
        // go into CLI mode
        return cliMode();
    } else {
        // run argv function
        string func;
        for (int i = 1; i < sizeof(argv); i++)
            if (argv[i] == nullptr)
                break;
            else func += string(argv[i]);
        return eval(&func);
    }
}

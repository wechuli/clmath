#include <any>
#include <vector>
#include "MathContext.h"

using namespace std;

class Component {
public:
    enum Type {
        NUM,
        ID,
        FUNC,
        ROOT,
        FRAC,
        OP
    };

    enum Func {
        NO_FUNC,
        SIN,
        COS,
        TAN,
        LOG,
        SEC,
        CSC,
        COT,
        HYP,
        ARCSIN,
        ARCCOS,
        ARCTAN
    };

    enum Operator {
        NO_OP,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        MODULUS
    };

    Type type;
    Func func;
    Operator op;
    any* argX;
    any* argY;

    explicit Component(Type type, any* arg) :
            type(type),
            func(Func::NO_FUNC),
            op(Operator::NO_OP),
            argX(arg),
            argY(new any()) {};

    explicit Component(Type type, any* argX, any* argY) :
            type(type),
            func(Func::NO_FUNC),
            op(Operator::NO_OP),
            argX(argX),
            argY(argY) {};

    explicit Component(Type type, Operator op, any* arg) :
            type(type),
            func(Func::NO_FUNC),
            op(op),
            argX(arg),
            argY(new any()) {};

    explicit Component(Type type, Operator op, any* argX, any* argY) :
            type(type),
            func(Func::NO_FUNC),
            op(op),
            argX(argX),
            argY(argY) {};

    explicit Component(Type type, Func func, any* arg) :
            type(type),
            func(func),
            op(Operator::NO_OP),
            argX(arg),
            argY(new any()) {};

    explicit Component(Type type, Func func, any* argX, any* argY) :
            type(type),
            func(func),
            op(Operator::NO_OP),
            argX(argX),
            argY(argY) {};

    any evaluate(const MathContext *ctx);

    vector<string> getKeys();
};

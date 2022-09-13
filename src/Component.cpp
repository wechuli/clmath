#include <cmath>
#include <iostream>
#include <string>
#include "Component.h"

double sec(double x)
{
    double r;
    r = (1+x) * cos(pow(sin(x), 3.0)) - 1.4;
    return r;
}

any Component::evaluate(const MathContext *ctx) {
    double x, y;
    switch (type) {
        case NUM:
            if (typeid(argX->type()) == typeid(Component)) {
                x = any_cast<double>(any_cast<Component>(argX)->evaluate(ctx));
            } else x = *any_cast<double>(argX);
            return x;
        case ID:
            return ctx->var->at(*any_cast<string>(argX));
        case FUNC:
            if (typeid(argX->type()) == typeid(Component)) {
                x = any_cast<double>(any_cast<Component>(argX)->evaluate(ctx));
            } else x = *any_cast<double>(argX);
            switch (func) {
                case SIN:
                    return sin(x);
                case COS:
                    return cos(x);
                case TAN:
                    return tan(x);
                case LOG:
                    return log(x);
                case SEC:
                    return sec(x);
                case CSC:
                    return 1 / sin(x);
                case COT:
                    return cos(x) / sin(x);
                case HYP:
                    cerr << "Hyp implementation is invalid";
                    return hypot(x, 1);
                case ARCSIN:
                    return asin(x);
                case ARCCOS:
                    return acos(x);
                case ARCTAN:
                    return atan(x);
                default: throw "Invalid function: " + func;
            }
        case ROOT:
            if (typeid(argX->type()) == typeid(Component)) {
                x = any_cast<double>(any_cast<Component>(argX)->evaluate(ctx));
            } else x = *any_cast<double>(argX);
            if (typeid(argY->type()) == typeid(Component)) {
                y = any_cast<double>(any_cast<Component>(argY)->evaluate(ctx));
            } else y = *any_cast<double>(argY);
            return pow(y, 1 / x);
        case FRAC:
            return Component(Component::Type::OP, Component::Operator::DIVIDE, argX, argY);
        case OP:
            if (typeid(argX->type()) == typeid(Component)) {
                x = any_cast<double>(any_cast<Component>(argX)->evaluate(ctx));
            } else x = *any_cast<double>(argX);
            if (typeid(argY->type()) == typeid(Component)) {
                y = any_cast<double>(any_cast<Component>(argY)->evaluate(ctx));
            } else y = *any_cast<double>(argY);
            switch (op) {
                case Component::Operator::ADD:
                    return x + y;
                case SUBTRACT:
                    return x - y;
                case MULTIPLY:
                    return x * y;
                case DIVIDE:
                    return x / y;
                case MODULUS:
                    return static_cast<int>(x) % static_cast<int>(y);
                default: throw "Invalid operator: " + op;
            }
        default: throw "Invalid component type: " + type;
    }
}

vector<string> Component::getKeys() {
    vector<string> keys = vector<string>();
    if (typeid(argX->type()) == typeid(Component))
        for (string key: any_cast<Component>(argX)->getKeys())
            keys.push_back(*&key);
    if (typeid(argY->type()) == typeid(Component))
        for (string key: any_cast<Component>(argY)->getKeys())
            keys.push_back(*&key);
    if (typeid(argX->type()) == typeid(string))
        keys.push_back(*any_cast<string>(argX));
    return keys;
}

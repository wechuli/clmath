#include "FuncVisitor.h"
#include "Component.h"

any parseNumber(MathParser::NumContext* num) {
    if (num->DOT() == nullptr)
        return atoi(num->getText().c_str());
    else return atof(num->getText().c_str());
}

any FuncVisitor::visitFrac(MathParser::FracContext *ctx) {
    auto x = visit(ctx->x);
    auto y = visit(ctx->y);
    return Component(Component::Type::FRAC, &x, &y);
}

any FuncVisitor::visitFx(MathParser::FxContext *ctx) {
    auto x = visit(ctx->x);
    Component::Func func;
    switch (ctx->FUNC()->getSymbol()->getTokenIndex()) {
        case MathParser::SIN:
            func = Component::Func::SIN;
            break;
        case MathParser::COS:
            func = Component::Func::COS;
            break;
        case MathParser::TAN:
            func = Component::Func::TAN;
            break;
        case MathParser::LOG:
            func = Component::Func::LOG;
            break;
        case MathParser::SEC:
            func = Component::Func::SEC;
            break;
        case MathParser::CSC:
            func = Component::Func::CSC;
            break;
        case MathParser::COT:
            func = Component::Func::COT;
            break;
        case MathParser::HYP:
            func = Component::Func::HYP;
            break;
        case MathParser::ARCSIN:
            func = Component::Func::ARCSIN;
            break;
        case MathParser::ARCCOS:
            func = Component::Func::ARCCOS;
            break;
        case MathParser::ARCTAN:
            func = Component::Func::ARCTAN;
            break;
        default: throw "Invalid function: " + ctx->FUNC()->getText();
    }
    return Component(Component::Type::FUNC, func, &x);
}

any FuncVisitor::visitRoot(MathParser::RootContext *ctx) {
    any i;
    if (ctx->i == nullptr)
        i = 2;
    else i = visit(ctx->i);
    auto x = visit(ctx->x);
    return Component(Component::Type::ROOT, &x, &i);
}

any FuncVisitor::visitExprId(MathParser::ExprIdContext *ctx) {
    auto id = ctx->getText();
    return Component(Component::Type::ID, new any(id));
}

any FuncVisitor::visitExprNum(MathParser::ExprNumContext *ctx) {
    auto num = parseNumber(ctx->num());
    return Component(Component::Type::NUM, &num);
}

any FuncVisitor::visitExprOp(MathParser::ExprOpContext *ctx) {
    auto l = visit(ctx->l);
    auto r = visit(ctx->r);
    Component::Operator op;
    switch (ctx->OP()->getSymbol()->getTokenIndex()) {
        case MathParser::OP_ADD:
            op = Component::Operator::ADD;
            break;
        case MathParser::OP_SUB:
            op = Component::Operator::SUBTRACT;
            break;
        case MathParser::OP_MUL:
            op = Component::Operator::MULTIPLY;
            break;
        case MathParser::OP_DIV:
            op = Component::Operator::DIVIDE;
            break;
        case MathParser::OP_MOD:
            op = Component::Operator::MODULUS;
            break;
        default: throw "Invalid operator: " + ctx->OP()->getText();
    }
    return Component(Component::Type::OP, op, &l, &r);
}

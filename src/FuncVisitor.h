#include "AbstractVisitor.h"

using namespace std;

class FuncVisitor : public AbstractVisitor {
    any visitFrac(MathParser::FracContext *ctx) override;

    any visitFx(MathParser::FxContext *ctx) override;

    any visitRoot(MathParser::RootContext *ctx) override;

    any visitExprId(MathParser::ExprIdContext *ctx) override;

    any visitExprNum(MathParser::ExprNumContext *ctx) override;

    any visitExprOp(MathParser::ExprOpContext *ctx) override;
};

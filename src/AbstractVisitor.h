#include "MathBaseVisitor.h"

class AbstractVisitor : public MathBaseVisitor {
    bool shouldVisitNextChild(antlr4::tree::ParseTree *tree, const std::any &any) override {
        return any.has_value();
    }
};

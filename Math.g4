grammar Math;

OP_ADD: '+';
OP_SUB: '-';
OP_MUL: '*';
OP_DIV: '/';
OP_MOD: '%';

SIN: 'sin';
COS: 'cos';
TAN: 'tan';
LOG: 'log';
SEC: 'sec';
CSC: 'csc';
COT: 'cot';
HYP: 'hyp';
ARCSIN: 'a' 'rc'? SIN;
ARCCOS: 'a' 'rc'? COS;
ARCTAN: 'a' 'rc'? TAN;

func
    : SIN
    | COS
    | TAN
    | LOG
    | SEC
    | CSC
    | COT
    | HYP
    | ARCSIN
    | ARCCOS
    | ARCTAN
;

ROOT: 'sqrt' | 'root';
POW: '^';
FACTORIAL: '!';
FRAC: 'frac';

PAR_L: '(';
PAR_R: ')';
IDX_L: '[';
IDX_R: ']';
ACC_L: '{';
ACC_R: '}';

DOT: '.' | ',';
SEMICOLON: ';';
DOLLAR: '$';
EQUALS: '=';
DIGIT: [0-9];
num: OP_SUB? DIGIT+ (DOT DIGIT+)?;
CHAR: [a-zA-Z_];
word: CHAR+;
evalVar: name=word EQUALS expr;
eval: DOLLAR name=word (ACC_L evalVar (SEMICOLON evalVar)* ACC_R)?;

WS: [ \n\r\t] -> channel(HIDDEN);

parExpr: PAR_L n=expr PAR_R;
idxExpr: IDX_L n=expr IDX_R;

op
    : OP_ADD
    | OP_SUB
    | OP_MUL
    | OP_DIV
    | OP_MOD
    | POW
;

// functions
frac: FRAC x=parExpr y=parExpr;
fx: func x=parExpr;
root: ROOT i=idxExpr? x=parExpr;

// expressions
expr
    : l=expr op r=expr  #exprOp
    | parExpr           #exprPar
    | frac              #exprFrac
    | fx                #exprFunc
    | x=expr FACTORIAL  #exprFact
    | root              #exprRoot
    | num               #exprNum
    | word              #exprId
    | eval              #exprEval
;

UNMATCHED: . ; // raise errors on unmatched

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

FUNC
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
PI: 'pi';
POW: '^';
FRAC: 'frac';

PAR_L: '(';
PAR_R: ')';
IDX_L: '[';
IDX_R: ']';

DOT: '.' | ',';
DIGIT: [0-9];
num: OP_SUB? DIGIT+ (DOT DIGIT+)?;
CHAR: [a-zA-Z];
word: CHAR+;

WS: [ \n\r\t] -> channel(HIDDEN);

parExpr: PAR_L n=expr PAR_R;
idxExpr: IDX_L n=expr IDX_R;

OP
    : OP_ADD
    | OP_SUB
    | OP_MUL
    | OP_DIV
    | OP_MOD
    | POW
;

// functions
frac: FRAC x=parExpr y=parExpr;
fx: FUNC x=parExpr;
root: ROOT i=idxExpr? x=parExpr;

// expressions
expr
    : l=expr OP r=expr  #exprOp
    | frac              #exprFrac
    | fx                #exprFunc
    | root              #exprRoot
    | num               #exprNum
    | word              #exprId
;

UNMATCHED: . ; // raise errors on unmatched

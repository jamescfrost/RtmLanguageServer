parser grammar RtmParser;

options {
    tokenVocab = RtmLexer;
}

start
    : SOURCE_DESCRIPTION source_member* source_end? EOF
    ;

source_member
    : name_definition
    | overlay
    | NL
    ;

source_end
    : DOLLAR_END
    ;

name_definition
    : DOLLAR_NAME (NAME_ID | NAME_END) FREE_TEXT*
    ;

name_include
    : DOLLAR_INCLUDE INCLUDE_FILENAME (LB INCLUDE_NAME RB?)?
    ;

name_included_start
    : DOLLAR_INCLUDE_START INCLUDE_FILENAME (LB INCLUDE_NAME RB?)?
    ;

name_included_end
    : DOLLAR_INCLUDE_END
    ;

name_includes
    : name_include
    | name_included_start
    | name_included_end
    ;

overlay
    : overlay_name overlay_member*
    ;

overlay_member
    : files
    | ext
    | data_area
    | prog
    | proc
    | abort
    | name_includes
    | statement
    | NL
    ;

overlay_name
    : DOLLAR_ENTRY ID
    ;

files: DOLLAR_FILES ((ID | name_includes) COMMA?)*;
ext: DOLLAR_EXT ((ID | name_includes) COMMA?)*;

data_area
    : (
          DOLLAR_DATA (COMMA SHARED)?
        | DOLLAR_EXTDATA
        | DOLLAR_SCRNDATA
        | DOLLAR_USERDATA
      )
      NL*
      data_declarations?
    ;

data_declarations
    : data_declaration ((COMMA | NL)* data_declaration)*
    ;

data_declaration
    : data_field
    | name_includes
    ;

data_field
    : (DATA_ID_OR_MASK | FIELD_ID | FILL)
      (
          edit_mask (STAR NUMERIC_LITERAL)*
        | (STAR NUMERIC_LITERAL)* group_mask
        | EQUAL (DATA_ID_OR_MASK | FIELD_ID) edit_mask? (STAR NUMERIC_LITERAL)* group_mask?
      )?
    ;

edit_mask
    : DATA_ID_OR_MASK
    | copy_mask
    | code_string_mask
    | group_mask
    ;

copy_mask
    : COPY_MASK (PREFIX (ID | DATA_ID_OR_MASK))?
    ;

group_mask
    : NL* LSB ((COMMA | NL)* data_field (COMMA | NL)*)+ NL* RSB
    ;

code_string_mask
    : CODE_STRING_START
      (
          (WS* NL (NL | WS)* code_string_delim)?
          code_string_value
          (code_string_delim code_string_value)*
      )
      CODE_STRING_END
    ;

code_string_delim
    : HAT (WS* NL (NL | WS)* HAT)?
    ;

code_string_value
    : (CODE_STRING_VALUE | WS)+
    ;

parameter_list
    : (assignable | STAR) (COMMA* (assignable | STAR))*
    ;

call_parameter_list
    : LB argument_list? (COLON call_return_parameter_list?)? RB
    ;

argument_list
    : argument (COMMA+ argument)* COMMA?
    | COMMA+
    ;

call_return_parameter_list
    : call_return_parameter (COMMA? call_return_parameter)*
    ;

call_return_parameter
    : assignable
    | IGNORED_RETURN
    | STAR
    | ID EQUAL (STAR (MINUS | PLUS))? expression
    ;

argument
    : RTMFILE_NAME
    | STAR
    | LB argument_list RB
    | STAR (MINUS | PLUS) expression
    | (MINUS | PLUS) expression
    | REPEAT
    | APOSTROPHE? expression
    ;

return_statement_list
    : LB (expression (COMMA+ expression)*)? RB
    ;

prog
    : DOLLAR_PROG LB parameter_list? RB prog_member*
    ;

prog_member
    : name_includes
    | statement
    | NL
    ;

procs
    : proc+
    ;

proc
    : ID PROC (LB parameter_list? RB)? proc_member* ENDPROC terminator_statement*
    ;

proc_member
    : name_includes
    | statement
    | NL
    ;

abort
    : DOLLAR_ABORT (LB parameter_list? RB)? abort_member*
    ;

abort_member
    : name_includes
    | statement
    | NL
    ;

terminator_statement
    : return
    | QUITZUG
    | DIE LB argument RB
    ;

conditional
    : LB conditional RB
    | expression (
          EQUAL
        | NOT_EQUAL
        | LESS_THAN
        | MORE_THAN
        | LESS_OR_EQUAL
        | MORE_OR_EQUAL
      ) expression conditional_chain*
    | conditional (AND | OR) conditional conditional_chain*
    ;

conditional_chain
    : (AND | OR) (
          EQUAL
        | NOT_EQUAL
        | LESS_THAN
        | MORE_THAN
        | LESS_OR_EQUAL
        | MORE_OR_EQUAL
      ) expression
    ;

expression
    : LB expression RB                                                        # parenthesis
    | assignable (ASSIGN | EQUAL | ADD_TO) expression                         # assignmentExpr
    | op = (MINUS | INCREMENT | DECREMENT) expression                         # unary
    | left = expression op = (BIT_AND | BIT_OR | BIT_XOR) right = expression  # infix
    | left = expression op = (BIT_SHIFT_LEFT | BIT_SHIFT_RIGHT) right = expression # infix
    | left = expression op = (
          STAR
        | SLASH
        | DOUBLE_SLASH
        | SLASH_COLON
      ) right = expression                                                    # infix
    | left = expression op = (PLUS | MINUS) right = expression                # infix
    | value = function_call                                                   # function
    | value = if_expression                                                   # if
    | value = case_expression                                                 # case
    | value = expression bracket_expression                                   # bracket
    | value = literal                                                         # literalExpr
    | value = identifier_expression                                           # identifierExpr
    ;

identifier_expression
    : ID
    | FIELD_ID
    | AT_VARIABLE
    ;

statement
    : expression
    | do_block
    | while_statement
    | repeat_statement
    | until_statement
    | always_statement
    | never_statement
    | terminator_statement
    | SEMI_COLON
    | name_includes
    | COMMA
    | HAT
    | NL
    ;

do_block
    : DO statement* END
    ;

while_statement
    : WHILE conditional statement
    ;

until_statement
    : UNTIL conditional statement
    ;

repeat_statement
    : REPEAT statement
    ;

always_statement
    : ALWAYS statement
    ;

never_statement
    : NEVER statement
    ;

assignable
    : (FIELD_ID | ID | AT_VARIABLE) bracket_expression?
    ;

bracket_expression
    : LSB (expression | bracket_expression | STAR)
      (COMMA* (expression | bracket_expression | STAR))* RSB
    ;

if_expression
    : IF conditional THEN? statement (ELSE statement)?
    ;

case_expression
    : CASE expression statement+ ENDCASE
    ;

function_call
    : (ID | OVERLAY ID) call_parameter_list
    ;

literal
    : NUMERIC_LITERAL
    | STRING_LITERAL
    | DATE_LITERAL
    | HEX_LITERAL
    | KEY_LITERAL
    ;

return
    : RETURN return_statement_list
    ;
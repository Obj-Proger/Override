// The code editor of the site: CodeMirror 6 with C# and PostgreSQL, colored like the rest of the code.
// Built into src/Override.Web/wwwroot/lib/editor/editor.js with `npm run build`; the site loads it on demand.

import { autocompletion, closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import { defaultKeymap, history, historyKeymap, indentWithTab } from "@codemirror/commands";
import { PostgreSQL, sql } from "@codemirror/lang-sql";
import {
    HighlightStyle,
    StreamLanguage,
    bracketMatching,
    indentOnInput,
    indentUnit,
    syntaxHighlighting,
} from "@codemirror/language";
import { clike } from "@codemirror/legacy-modes/mode/clike";
import { EditorState } from "@codemirror/state";
import { EditorView, drawSelection, highlightActiveLine, keymap, lineNumbers } from "@codemirror/view";
import { tags } from "@lezer/highlight";

const words = (text) => Object.fromEntries(text.split(" ").map((word) => [word, true]));

// C# for the editor, colored the way Visual Studio does it as far as a tokenizer can tell: built-in types such as
// int and object are keywords, control flow keywords have their own color, a name before "(" is a method, a name
// declared after a type is a local, and a capitalized name that is not a member is a type.
// The site's build uses Roslyn for exact colors; the editor has no compiler at hand.
const csharpBase = clike({
    name: "csharp",
    keywords: words(
        "abstract as base bool byte char checked class const decimal default delegate double enum event explicit " +
        "extern false fixed float implicit in int interface internal is lock long namespace new null object operator " +
        "out override params private protected public readonly record ref sbyte sealed short sizeof stackalloc static " +
        "string struct this true typeof uint ulong unchecked unsafe ushort using var virtual void volatile get set init " +
        "async nameof nint nuint"),
    builtin: words(
        "if else switch case do for foreach while break continue return goto throw try catch finally yield await"),
    types: words(
        "Action ArrayList Console Dictionary Exception Func HashSet IComparable IDisposable IEnumerable IEnumerator " +
        "IList List Math Nullable Queue Span Stack String StringBuilder Task TimeSpan DateTime Guid Array Enum Object " +
        "Int32 Int64 Boolean Char Double Decimal InvalidCastException InvalidOperationException ArgumentException " +
        "ArgumentNullException NotImplementedException NotSupportedException KeyValuePair Lazy ValueTask"),
    blockKeywords: words("catch class do else finally for foreach if struct switch try while record interface enum namespace"),
    defKeywords: words("var"),
    atoms: words("true false null"),
    hooks: {
        "@": (stream, state) => {
            if (stream.eat('"')) {
                state.tokenize = verbatimString;
                return verbatimString(stream, state);
            }

            stream.eatWhile(/[\w$_]/);
            return "meta";
        },
    },
});

function verbatimString(stream, state) {
    let next;
    while ((next = stream.next()) != null) {
        if (next === '"' && !stream.eat('"')) {
            state.tokenize = null;
            break;
        }
    }

    return "string";
}

// Built-in types: a name right after one of them is being declared.
const typeKeywords = new Set(
    "bool byte char decimal double float int long object sbyte short string uint ulong ushort var nint nuint dynamic"
        .split(" "));

// Punctuation that can stand between a type and the declared name: generic arguments, arrays, nullable.
const typePunctuation = /^[<>?\[\],]+$/;

const callFollows = /^\s*(<[\w\s,<>]*>)?\s*\(/;

// Locals and parameters are colored on every use, as in Visual Studio. The editor has no compiler, so it
// remembers lower-case names declared after a type ("long sum", "object item", "ArrayList numbers") and colors
// those names further on. Capitalized names after a type are fields and properties, which stay uncolored.
function copyState(state) {
    const copy = csharpBase.copyState ? csharpBase.copyState(state) : { ...state };
    copy.locals = new Set(state.locals);
    return copy;
}

const csharp = {
    ...csharpBase,
    startState(indentUnit) {
        const state = csharpBase.startState(indentUnit);
        state.locals = new Set();
        state.afterType = false;
        return state;
    },
    copyState,
    token(stream, state) {
        const style = csharpBase.token(stream, state);
        const text = stream.current();

        // "builtin" already has a meaning in CodeMirror's legacy token names, so control keywords are
        // returned under the name of their highlighting tag instead.
        if (style === "builtin") {
            state.afterType = false;
            return "controlKeyword";
        }

        if (style === "keyword") {
            state.afterType = typeKeywords.has(text);
            return style;
        }

        if (style === "type") {
            state.afterType = true;
            return style;
        }

        if (style === "variable" || style === "def") {
            const afterType = state.afterType;
            const isMember = stream.string.charAt(stream.start - 1) === ".";
            state.afterType = false;

            if (stream.match(callFollows, false))
                return "function";

            if (!isMember && /^[A-Z]/.test(text)) {
                state.afterType = true;
                return "type";
            }

            if (!isMember && /^[a-z_]/.test(text) && (afterType || state.locals.has(text))) {
                state.locals.add(text);
                return "def";
            }

            return "variable";
        }

        // Spaces keep the type in mind; other punctuation and operators end it, except inside a type.
        if ((style == null || style === "operator") && (/^\s+$/.test(text) || typePunctuation.test(text)))
            return style;

        state.afterType = false;
        return style;
    },
    tokenTable: {
        function: tags.function(tags.variableName),
    },
};

// Colors come from the site's CSS variables, so the editor follows the light and dark theme.
const highlight = HighlightStyle.define([
    { tag: tags.controlKeyword, color: "var(--c-kw-ctrl)" },
    { tag: tags.keyword, color: "var(--c-kw)" },
    { tag: [tags.typeName, tags.className, tags.namespace], color: "var(--c-type)" },
    { tag: [tags.string, tags.character, tags.special(tags.string)], color: "var(--c-str)" },
    { tag: tags.number, color: "var(--c-num)" },
    { tag: [tags.comment, tags.lineComment, tags.blockComment], color: "var(--c-cmt)" },
    { tag: tags.function(tags.variableName), color: "var(--c-method)" },
    { tag: tags.definition(tags.variableName), color: "var(--c-local)" },
    { tag: [tags.meta, tags.processingInstruction], color: "var(--c-pre)" },
]);

const theme = EditorView.theme({
    "&": {
        color: "var(--code-text)",
        backgroundColor: "var(--code-bg)",
        border: "1px solid var(--border-strong)",
        borderRadius: "8px",
        fontSize: "14px",
    },
    "&.cm-focused": { outline: "2px solid var(--accent)", outlineOffset: "1px" },
    ".cm-scroller": { fontFamily: "var(--mono)", lineHeight: "1.6", minHeight: "120px" },
    ".cm-content": { caretColor: "var(--strong)", padding: "10px 0" },
    ".cm-gutters": { backgroundColor: "var(--code-bg)", color: "var(--muted)", border: "none" },
    ".cm-activeLine": { backgroundColor: "color-mix(in srgb, var(--accent) 6%, transparent)" },
    ".cm-activeLineGutter": { backgroundColor: "transparent", color: "var(--text)" },
    "&.cm-focused .cm-cursor": { borderLeftColor: "var(--strong)" },
    "&.cm-focused .cm-selectionBackground, .cm-selectionBackground": {
        backgroundColor: "color-mix(in srgb, var(--accent) 30%, transparent)",
    },
});

const editors = new Map();
let nextId = 1;

/** Creates an editor inside host and returns its id for the other functions. */
export function create(host, text, language) {
    const view = new EditorView({
        parent: host,
        state: EditorState.create({
            doc: text,
            extensions: [
                lineNumbers(),
                history(),
                drawSelection(),
                highlightActiveLine(),
                indentOnInput(),
                bracketMatching(),
                closeBrackets(),
                autocompletion({ activateOnTyping: false }),
                indentUnit.of("    "),
                EditorState.tabSize.of(4),
                keymap.of([...closeBracketsKeymap, ...defaultKeymap, ...historyKeymap, indentWithTab]),
                language === "sql" ? sql({ dialect: PostgreSQL }) : StreamLanguage.define(csharp),
                syntaxHighlighting(highlight),
                theme,
                EditorView.contentAttributes.of({ "aria-label": "Редактор кода", spellcheck: "false" }),
            ],
        }),
    });

    const id = nextId++;
    editors.set(id, view);
    return id;
}

export function getText(id) {
    return editors.get(id)?.state.doc.toString() ?? "";
}

export function setText(id, text) {
    const view = editors.get(id);
    view?.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: text } });
}

export function dispose(id) {
    editors.get(id)?.destroy();
    editors.delete(id);
}

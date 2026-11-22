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
import { csharp } from "@codemirror/legacy-modes/mode/clike";
import { EditorState } from "@codemirror/state";
import { EditorView, drawSelection, highlightActiveLine, keymap, lineNumbers } from "@codemirror/view";
import { tags } from "@lezer/highlight";

// Colors come from the site's CSS variables, so the editor follows the light and dark theme.
const highlight = HighlightStyle.define([
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

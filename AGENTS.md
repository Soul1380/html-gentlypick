# Shared interface standards

All new path fields, option selectors and secondary windows must reuse `app/ui-components.js` and `app/ui-components.css`.

- Use `GPUI.path(input, options)` for paths. Keep the Enter action inside the right edge of the field. Use the shared desktop folder dialog with an address bar, never `FolderBrowserDialog`.
- Preserve native dropdowns for short option sets such as collection type, entry mode, sort order, quality and output format. Search dialogs must opt in with a variant or `data-ui-picker`; never globally convert all selects.
- Use `GPUI.variant(id, options)` before inserting a select to add a title, completion handler or special actions. Reuse the base selector; do not copy it to another page.
- Compose product workflows with `GPUI.dialog`. Model management must preserve the calling page's DOM and return with one click or Escape.
- Keep unsaved drafts in memory. Never write API keys to local storage or draft files.
- Refresh only reads state. It must not launch an installer, import a model or open native windows.
- Keep model source paths and Ollama storage paths distinct. Provide direct open-location actions.
- Show status with both text and the shared color palette. Keep real download counters; use indeterminate bars when progress is unknown.
- Select articles by project groups. Never silently drop a project's articles because it contains more than 20 items.
- Preserve mouse/keyboard focus accessibility: mouse clicks must not leave a toolbar focus rectangle; keyboard focus stays visible.

For UI changes run `node tests/ui-v110.cjs` and `node tests/shared-components.cjs`, plus the relevant domain tests. Check desktop/narrow layouts and both themes. Native desktop changes must compile successfully; folder selection must share `desktop/SharedControls.cs` across the app and installer.

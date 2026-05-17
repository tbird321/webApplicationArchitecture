import React, { useState, useEffect } from 'react';
import { ModalDialog, ThemeBuilder, availableCSSAttributes } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import Button from "@mui/material/Button";
import { DataGrid } from "@mui/x-data-grid";
import { nanoid } from "nanoid";

const selectorColumns = [
    { field: "name", Header: "Name", Type: "Text" },
    { field: "type", Header: "Type", Type: "Text" },
];

const pageSize = [2, 5, 10, 20, 50, 100];

const defaultTheme = {
    id: nanoid(),
    name: "",
    selectors: []
};

const ThemeGrid = ({ themesList }) => {
    const { Authorization, FileProcessing } = useAppStateContext();
    const [workingThemes, setWorkingThemes] = useState(themesList);
    const [currentTheme, setCurrentTheme] = useState(null);
    const [themeFileName, setThemeFileName] = useState("");
    // removed newThemeName local state; we pass prompt value directly to handler
    const [showModal, setShowModal] = useState(false);
    const [selectors, setSelectors] = useState([]);

    useEffect(() => {
        setWorkingThemes(themesList);
    }, [themesList]);

    const generateColumns = () => {
        const columnDefinitions = selectorColumns.map(column => {
            let columnDef = {
                field: column.field,
                headerName: column.Header,
                flex: 1.5,
                type: column.Type,
            };
            if (column.field === "type") {
                columnDef.renderCell = (params) => {
                    return params.row.type ? params.row.type.label : "";
                };
            }
            return columnDef;
        });

        columnDefinitions.push({
            field: "delete",
            headerName: "",
            flex: 1,
            renderCell: (params) => (
                <Button
                    color="error"
                    onClick={(event) => {
                        event.stopPropagation(); // Prevent event from bubbling up
                        handleDelete(params.row.id);
                    }}
                >
                    Delete
                </Button>
            ),
        });

        return columnDefinitions;
    };

    const handleDelete = async (themeId) => {
        if (!window.confirm('Delete this theme CSS file? This cannot be undone.')) return;
        const themeToDelete = workingThemes.find(theme => theme.id === themeId);
        if (!themeToDelete) {
            console.error("Theme not found");
            return;
        }
        const folderPath = 'assets/' + Authorization.getConfiguration().Site.siteName + '/themes';
        const fileName = themeToDelete.name + '.css';
        const result = await FileProcessing.deleteFileData(folderPath, fileName);
        if (result) {
            console.log('File deleted successfully:', fileName);
            setWorkingThemes(workingThemes.filter(theme => theme.id !== themeId));
        }
    };

    const handleSaveSelectors = async (data) => {
        let cssContent = '';

        Object.keys(data).forEach(key => {
            const selector = data[key];
            let cssSelector = '';

            switch (selector.type.value) {
                case 'class':
                    cssSelector = `.${selector.name}`;
                    break;
                case 'Id':
                    cssSelector = `#${selector.name}`;
                    break;
                case 'tag':
                    cssSelector = selector.name;
                    break;
                default:
                    cssSelector = selector.name;
                    break;
            }

            let cssRules = selector.stylingElements.map(element => {
                return `\t${element.attName}: ${element.attValue};`;
            }).join('\n');

            cssContent += `${cssSelector} {\n${cssRules}\n}\n`;
        });

        const config = Authorization.getConfiguration();
        const folderPath = 'assets/' + config.Site.siteName + '/themes';
        const fileName = themeFileName + '.css';
        try {
            const result = await FileProcessing.saveFileData(folderPath, fileName, cssContent, 'text/css');
            if (result) {
                console.log('File saved successfully:', result);
                // Inject or refresh stylesheet for live preview
                const version = Date.now();
                const href = `/assets/${config.Site.siteName}/themes/${fileName}?v=${version}`;
                const existing = document.querySelector(`link[data-theme-css="${fileName}"]`);
                if (existing) {
                    existing.href = href;
                } else {
                    const link = document.createElement('link');
                    link.rel = 'stylesheet';
                    link.href = href;
                    link.setAttribute('data-theme-css', fileName);
                    document.head.appendChild(link);
                }
            }
        } catch (error) {
            console.error('Error saving file:', error);
        }
        setCurrentTheme(null);
    };

    const handleGetTheme = async (fileName) => {
        const config = Authorization.getConfiguration();
        const folderPath = 'assets/' + config.Site.siteName + '/themes';
        const fileNameWithExtension = fileName.endsWith('.css') ? fileName : `${fileName}.css`;

        try {
            const result = await FileProcessing.getFileData(folderPath, fileNameWithExtension);
            if (result) {
                const themeObject = parseCssToThemeObject(result, availableCSSAttributes);
                setSelectors(themeObject);
            }
        } catch (error) {
            console.error('Themes retrieval failed:', error);
        }
    };

    function parseCssToThemeObject(cssContent, availableCSSAttributes) {
        const themeObject = [];
        const cssBlocks = cssContent.split('}').filter(Boolean);

        cssBlocks.forEach((block, index) => {
            const [selectorText, rulesText] = block.split('{').map(s => s.trim());
            if (!selectorText || !rulesText) return;

            const selectorType = determineSelectorType(selectorText);
            const selector = {
                id: index + 1,
                name: selectorText.replace(/\.|#/, ''),
                type: selectorType,
                stylingElements: []
            };

            const rules = rulesText.split(';').filter(Boolean);
            rules.forEach((rule, ruleIndex) => {
                let [attName, attValue] = rule.split(':').map(s => s.trim());
                attName = attName.toLowerCase();

                if (attName && attValue) {
                    const attributeDetails = availableCSSAttributes.find(attr => attr.name === attName);
                    const attType = attributeDetails ? { label: attributeDetails.label, value: attributeDetails.type } : { label: 'Generic', value: 'generic' };

                    selector.stylingElements.push({
                        id: ruleIndex + 1,
                        attName,
                        attValue,
                        attType,
                    });
                }
            });
            themeObject.push(selector);
        });

        return themeObject;
    }

    function determineSelectorType(selectorText) {
        if (selectorText.startsWith('.')) {
            return { label: 'Class', value: 'class' };
        } else if (selectorText.startsWith('#')) {
            return { label: 'Id', value: 'id' };
        } else {
            return { label: 'Tag', value: 'tag' };
        }
    }

    const handleRowClick = async (selectionModel) => {
        if (selectionModel.row === currentTheme) {
            setCurrentTheme(null);
            setThemeFileName(null);
            setShowModal(false);
        } else {
            await handleGetTheme(selectionModel.row.name);
            setThemeFileName(selectionModel.row.name);
            setCurrentTheme(selectionModel.row);
            setShowModal(true);
        }
    };

    const handleNewTheme = (name) => {
        const safeName = String(name || '').trim();
        if (!safeName) return;
        const newTheme = {
            ...defaultTheme,
            name: safeName,
            id: safeName,
        };
        setCurrentTheme(newTheme);
        setWorkingThemes(prev => [...(prev || []), newTheme]);
        setThemeFileName(newTheme.name);
        setShowModal(true);
    };

    // removed unused handleNameChange

    return (
        <>
            <h3>Themes</h3>
            <DataGrid
                sx={{ "--unstable_DataGrid-radius": "0" }}
                rows={workingThemes || []}
                columns={generateColumns()}
                initialState={{}}
                rowHeight={54}
                onRowClick={handleRowClick}
                pageSizeOptions={pageSize}
            />
            <div style={{ marginTop: 8, marginBottom: 8 }}>
                <Button onClick={() => {
                    const name = prompt('Theme name (alphanumeric, dash/underscore):');
                    if (!name) return;
                    const valid = /^[A-Za-z0-9_-]+$/.test(name.trim());
                    if (!valid) {
                        alert('Invalid name. Use letters, numbers, dashes or underscores.');
                        return;
                    }
                    handleNewTheme(name.trim());
                }}>Create</Button>
                <Button onClick={() => {
                    if (!themeFileName) return;
                    handleGetTheme(themeFileName);
                }} style={{ marginLeft: 8 }}>Reload</Button>
            </div>

            {currentTheme && (
                <ModalDialog open={showModal} onClose={() => setShowModal(false)}>
                    <div style={{ minWidth: '900px', minHeight: '600px' }}>
                        <ThemeBuilder columns={selectorColumns} pageSize={pageSize} selectors={selectors} onSaveSelectors={handleSaveSelectors} />
                    </div>
                </ModalDialog>
            )}
        </>
    );
};

export default ThemeGrid;
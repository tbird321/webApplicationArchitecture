import { createTheme } from "@mui/material/styles";
export const blueThemeOptions  = {
    palette: {
        mode: "light",
        primary: {
            main: "#0874de",
        },
        secondary: {
            main: "#2de8de",
        },
        background: {
            default: "#fff",
            paper: "#fff",
        },
        error: {
            main: "#de1010",
        },
        success: {
            main: "#13c51a",
        },
    },
};
const blueTheme = createTheme(blueThemeOptions);
export default blueTheme;

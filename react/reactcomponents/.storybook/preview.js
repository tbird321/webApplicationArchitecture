import React from 'react';
import { ThemeProvider } from '@mui/material/styles';
import blueTheme from '../src/components/theme/blueTheme';

/** @type { import('@storybook/react').Preview } */
const preview = {
  parameters: {
    actions: { argTypesRegex: "^on[A-Z].*" },
    controls: {
      matchers: {
        date: /Date$/,
      },
    },
  },
  decorators: [(Story) => (
    <ThemeProvider theme={blueTheme}>
      <Story />
    </ThemeProvider>
  )],
};

export default preview;

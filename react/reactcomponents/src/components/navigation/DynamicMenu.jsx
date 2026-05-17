import React from "react";
import Button from "@mui/material/Button";
import ClickAwayListener from "@mui/material/ClickAwayListener";
import Grow from "@mui/material/Grow";
import Paper from "@mui/material/Paper";
import Popper from "@mui/material/Popper";
import MenuItem from "@mui/material/MenuItem";
import MenuList from "@mui/material/MenuList";
import Stack from "@mui/material/Stack";
import "./DynamicMenu.css";

function DynamicMenu({ parentMenuItem, onClick, keepOpen, showTitles = false }) {
    const [open, setOpen] = React.useState(false);
    const anchorRef = React.useRef(null);

    const handleToggle = () => {
        if (parentMenuItem?.onClick) {
            parentMenuItem?.onClick(parentMenuItem);
        }
        if (onClick) {
            onClick(parentMenuItem);
        }
        setOpen((prevOpen) => !prevOpen);
    };

    const handleClose = (event) => {
        if (event) {
            if (anchorRef.current && anchorRef.current.contains(event.target)) {
                return;
            }
        }
        setOpen(false);
    };

    function handleListKeyDown(event) {
        if (event.key === "Tab") {
            event.preventDefault();
            setOpen(false);
        } else if (event.key === "Escape") {
            setOpen(false);
        }
    }

    // return focus to the button when we transitioned from !open -> open
    const prevOpen = React.useRef(open);
    React.useEffect(() => {
        if (prevOpen.current === true && open === false) {
            anchorRef.current.focus();
        }

        prevOpen.current = open;
    }, [open]);

    return (
        <Stack direction="row" spacing={2} className="dynamic-menu">
            <div>
                <Button
                    ref={anchorRef}
                    className="parent-menu-button"
                    id={parentMenuItem.id}
                    aria-controls={open ? "composition-menu" : undefined}
                    aria-expanded={open ? "true" : undefined}
                    aria-haspopup="true"
                    title={showTitles ? (parentMenuItem?.itemTitle || undefined) : undefined}
                    onClick={handleToggle}
                >
                    {parentMenuItem?.text}
                </Button>
                <Popper
                    open={open}
                    anchorEl={anchorRef.current}
                    role={undefined}
                    placement="bottom-start"
                    transition
                    disablePortal
                    className="custom-popper"
                >
                    {({ TransitionProps, placement }) => (
                        <Grow
                            {...TransitionProps}
                            style={{
                                transformOrigin:
                                    placement === "bottom-start" ? "left top" : "left bottom",
                            }}
                        >
                            <Paper>
                                <ClickAwayListener onClickAway={(event) => {
                                    if (!keepOpen) {
                                        handleClose(event);
                                    }
                                }}>
                                    <MenuList
                                        autoFocusItem={open}
                                        id={"composition-menu_" + parentMenuItem?.id}
                                        aria-labelledby="composition-button"
                                        onKeyDown={handleListKeyDown}
                                    >
                                        {parentMenuItem?.menuItems?.map((item, index) => (
                                            <MenuItem
                                                id={item.id}
                                                key={index}
                                                title={showTitles ? (item?.itemTitle || undefined) : undefined}
                                                onClick={(event) => {
                                                    if (item && item.onClick) {
                                                        item.onClick(item);
                                                    }
                                                    if (onClick) {
                                                        onClick(item);
                                                    }
                                                    if (!keepOpen) {
                                                        handleClose(event);
                                                    }
                                                }}
                                            >
                                                {item.text}
                                            </MenuItem>
                                        ))}
                                    </MenuList>
                                </ClickAwayListener>
                            </Paper>
                        </Grow>
                    )}
                </Popper>
            </div>
        </Stack>
    );
}

export default DynamicMenu;


function getImage(imageName) {
    return require(`../images/${imageName}`);
}
const layoutArray = [
    {name:"Standard", diagram: getImage("1_1.png")},
    {name:"1, 1/3, 2/3 Grid", diagram: getImage("1_1-3_2-3.png")},
    {name:"1, 1/3 Grid", diagram: getImage("1_1-3.png")},
    {name:"1, 2, 3 Grid", diagram: getImage("1_2_3.png")},
    {name:"1, 2/3, 1/3 Grid", diagram: getImage("1_2-3_1-3.png")},
    {name:"1, 2/3 Grid", diagram: getImage("1_2-3.png")},
    {name:"1, 2 Grid", diagram: getImage("1_2.png")},
    {name:"1, 3 Grid", diagram: getImage("1_3.png")},
    {name:"1/3 Grid", diagram: getImage("1-3_1-3.png")},
    {name:"1/3, 1 Grid", diagram: getImage("1-3_1.png")},
    {name:"1/3, 2/3 Grid", diagram: getImage("1-3_2-3.png")},
    {name:"2, 1 Grid", diagram: getImage("2_1.png")},
    {name:"2 Grid", diagram: getImage("2_2.png")},
    {name:"2, 3 Grid", diagram: getImage("2_3.png")},
    {name:"2/3, 1 Grid", diagram: getImage("2-3_1.png")},
    {name:"2/3 Grid", diagram: getImage("2-3_2-3.png")},
    {name:"3, 1 Grid", diagram: getImage("3_1.png")},
    {name:"3, 2 Grid", diagram: getImage("3_2.png")},
    {name:"3 Grid", diagram: getImage("3_3.png")},
];

const availableLayouts = new Map();

layoutArray.forEach(layout => {
    availableLayouts.set(layout.name, layout.diagram);
});

export default availableLayouts;
